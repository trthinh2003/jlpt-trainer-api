using JlptTrainer.Application.AudioLessons.Lyrics;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.AudioLessons.Queries.GetAudioLessonPlayback;

public sealed record GetAudioLessonPlayback(Guid Id) : IRequest<AudioLessonPlayback>;
public sealed record AudioLessonPlayback(string AudioUrl, string CoverUrl, LyricLine[] Lyrics);

public sealed class GetAudioLessonPlaybackHandler(IApplicationDbContext db, IMediaObjectStorage storage, TimeProvider clock)
    : IRequestHandler<GetAudioLessonPlayback, AudioLessonPlayback>
{
    public async Task<AudioLessonPlayback> Handle(GetAudioLessonPlayback request, CancellationToken ct)
    {
        var expiresAt = clock.GetUtcNow() + AudioLessonMedia.PlaybackLifetime;
        var lesson = await db.PublishedAudioLessons().Where(lesson => lesson.Id == request.Id)
            .Select(item => new
            {
                item.ObjectKey, item.CoverKey, item.Lyrics,
                Words = item.Words
                    .Select(word => new AudioLessonVocab(word.VocabId, word.Vocab.Word, word.Vocab.Reading, word.Vocab.Meaning))
                    .ToList()
            })
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(AudioLesson), request.Id);
        AudioLessonMedia.RequireConfigured(storage);
        var audio = await storage.SignReadAsync(lesson.ObjectKey, expiresAt, ct);
        var cover = await storage.SignReadAsync(lesson.CoverKey, expiresAt, ct);
        return new AudioLessonPlayback(audio, cover, LyricVocabulary.Resolve(LrcLyrics.Parse(lesson.Lyrics), lesson.Words));
    }
}
