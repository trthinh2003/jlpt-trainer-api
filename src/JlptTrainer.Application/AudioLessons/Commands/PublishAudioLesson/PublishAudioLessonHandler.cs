using JlptTrainer.Application.AudioLessons.Lyrics;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.AudioLessons.Commands.PublishAudioLesson;

public sealed class PublishAudioLessonHandler(
    IApplicationDbContext db, IMediaObjectStorage storage, IMediaImageNormalizer images, TimeProvider clock)
    : IRequestHandler<PublishAudioLesson, Guid>
{
    public async Task<Guid> Handle(PublishAudioLesson request, CancellationToken ct)
    {
        AudioLessonMedia.RequireConfigured(storage);
        var lines = LrcLyrics.Parse(request.Lyrics);
        var words = await db.Vocabs.Where(word => request.VocabIds.Contains(word.Id))
            .Select(word => new AudioLessonVocab(word.Id, word.Word, word.Reading, word.Meaning)).ToListAsync(ct);
        if (words.Count != request.VocabIds.Length) throw new ConflictException(PublishAudioLesson.MissingVocabMessage);
        LyricVocabulary.Resolve(lines, words, requireLinks: true);
        using var normalizedCover = ReadCover(request.OpenCover);
        await using var audioStream = request.OpenAudio();
        var audio = AudioFile.Inspect(audioStream);
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

    private Stream ReadCover(Func<Stream> openCover)
    {
        using var source = openCover();
        return images.NormalizeToWebp(source);
    }

    private async Task RequireWordsAsync(Guid[] ids, CancellationToken ct)
    {
        if (await db.Vocabs.CountAsync(word => ids.Contains(word.Id), ct) != ids.Length)
            throw new ConflictException(PublishAudioLesson.MissingVocabMessage);
    }
}
