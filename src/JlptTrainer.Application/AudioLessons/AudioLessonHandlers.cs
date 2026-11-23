using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Common.Media;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.AudioLessons;

public sealed class AudioLessonHandlers(IApplicationDbContext db, IMediaObjectStorage storage, TimeProvider clock)
    : IRequestHandler<PublishAudioLesson, Guid>, IRequestHandler<ListAudioLessons, AudioLessonPage>,
      IRequestHandler<GetAudioLesson, AudioLessonPlayback>, IRequestHandler<GetAudioLessonWords, IReadOnlyList<AudioLessonVocab>>,
      IRequestHandler<UnpublishAudioLesson>
{
    private static readonly TimeSpan playbackLifetime = TimeSpan.FromMinutes(30);
    private const int pageSize = 20;

    public async Task<Guid> Handle(PublishAudioLesson request, CancellationToken ct)
    {
        EnsureStorage();
        LrcLyrics.Parse(request.Lyrics);
        using var normalizedCover = ReadCover(request.OpenCover);
        await using var audioStream = request.OpenAudio();
        var audio = AudioFile.Inspect(audioStream);
        await RequireWordsAsync(request.VocabIds, ct);

        var id = Guid.NewGuid();
        var prefix = $"media/audio-lessons/{id:N}";
        var lesson = new AudioLesson
        {
            Id = id, Title = request.Title.Trim(), Level = request.Level, Lyrics = request.Lyrics,
            ObjectKey = $"{prefix}/audio.{audio.Extension}", CoverKey = $"{prefix}/cover.webp",
            CleanupAfter = clock.GetUtcNow().AddHours(1)
        };
        db.AudioLessons.Add(lesson);
        await db.SaveChangesAsync(ct);
        await storage.UploadAsync(lesson.ObjectKey, audioStream, audio.ContentType, ct);
        await storage.UploadAsync(lesson.CoverKey, normalizedCover, "image/webp", ct);

        await RequireWordsAsync(request.VocabIds, ct);
        var now = clock.GetUtcNow();
        if (now >= lesson.CleanupAfter) throw new ConflictException("Lượt tải bài nghe đã hết hạn. Vui lòng thử lại.");
        lesson.PublishedAt = now;
        lesson.Words.AddRange(request.VocabIds.Select((vocabId, index) =>
            new AudioLessonWord { VocabId = vocabId, Position = index }));
        await db.SaveChangesAsync(ct);
        return id;
    }

    public async Task<AudioLessonPage> Handle(ListAudioLessons request, CancellationToken ct)
    {
        var query = db.AudioLessons.Where(lesson => lesson.PublishedAt != null);
        if (request.Level is { } level) query = query.Where(lesson => lesson.Level == level);
        var count = await query.CountAsync(ct);
        long offset = (request.PageNumber - 1L) * pageSize;
        List<AudioLessonItem> items = offset >= count ? [] : await query
            .OrderByDescending(lesson => lesson.PublishedAt).ThenBy(lesson => lesson.Id)
            .Skip((int)offset).Take(pageSize)
            .Select(lesson => new AudioLessonItem(lesson.Id, lesson.Title, lesson.Level)).ToListAsync(ct);
        return new AudioLessonPage(items, count, request.PageNumber, pageSize);
    }

    public async Task<AudioLessonPlayback> Handle(GetAudioLesson request, CancellationToken ct)
    {
        var expiresAt = clock.GetUtcNow() + playbackLifetime;
        var lesson = await PublishedLesson(request.Id)
            .Select(item => new { item.ObjectKey, item.CoverKey, item.Lyrics })
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(AudioLesson), request.Id);
        EnsureStorage();
        var audio = await storage.SignReadAsync(lesson.ObjectKey, expiresAt, ct);
        var cover = await storage.SignReadAsync(lesson.CoverKey, expiresAt, ct);
        return new AudioLessonPlayback(audio, cover, LrcLyrics.Parse(lesson.Lyrics));
    }

    public async Task<IReadOnlyList<AudioLessonVocab>> Handle(GetAudioLessonWords request, CancellationToken ct)
    {
        var words = await PublishedLesson(request.Id)
            .Select(lesson => lesson.Words.OrderBy(word => word.Position)
                .Select(word => new AudioLessonVocab(word.VocabId, word.Vocab.Word, word.Vocab.Reading, word.Vocab.Meaning))
                .ToList())
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(AudioLesson), request.Id);
        return words;
    }

    public async Task Handle(UnpublishAudioLesson request, CancellationToken ct)
    {
        var retireAfter = clock.GetUtcNow() + playbackLifetime;
        var changed = await PublishedLesson(request.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(lesson => lesson.PublishedAt, (DateTimeOffset?)null)
                .SetProperty(lesson => lesson.CleanupAfter, retireAfter), ct);
        if (changed == 0) throw new NotFoundException(nameof(AudioLesson), request.Id);
    }

    private IQueryable<AudioLesson> PublishedLesson(Guid id) =>
        db.AudioLessons.Where(lesson => lesson.Id == id && lesson.PublishedAt != null);

    private static MemoryStream ReadCover(Func<Stream> openCover)
    {
        using var source = openCover();
        return ImageFile.Normalize(source);
    }

    private async Task RequireWordsAsync(Guid[] ids, CancellationToken ct)
    {
        if (await db.Vocabs.CountAsync(word => ids.Contains(word.Id), ct) != ids.Length)
            throw new ConflictException("Có từ đã bị gỡ khỏi kho. Vui lòng chọn lại.");
    }

    private void EnsureStorage()
    {
        if (!storage.IsConfigured) throw new ExternalServiceException("S3", "Kho audio chưa được cấu hình.");
    }
}
