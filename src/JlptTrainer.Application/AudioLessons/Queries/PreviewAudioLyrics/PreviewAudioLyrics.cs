using FluentValidation;
using JlptTrainer.Application.AudioLessons.Lyrics;
using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.AudioLessons.Queries.PreviewAudioLyrics;

public sealed record PreviewAudioLyrics(string Lyrics, Guid[]? VocabIds = null) : IRequest<LyricLine[]>;

public sealed class PreviewAudioLyricsValidator : AbstractValidator<PreviewAudioLyrics>
{
    public PreviewAudioLyricsValidator()
    {
        RuleFor(request => request.Lyrics).WithLrcTextLimits();
        RuleFor(request => request.VocabIds).Must(ids => ids is null || ids.Length <= 20)
            .WithMessage("Chọn tối đa 20 mục từ vựng.");
    }
}

public sealed class PreviewAudioLyricsHandler(IApplicationDbContext db) : IRequestHandler<PreviewAudioLyrics, LyricLine[]>
{
    public async Task<LyricLine[]> Handle(PreviewAudioLyrics request, CancellationToken ct)
    {
        var lines = LrcLyrics.Parse(request.Lyrics);
        List<AudioLessonVocab> words = request.VocabIds is { Length: > 0 } ids
            ? await db.Vocabs.Where(word => ids.Contains(word.Id))
                .Select(word => new AudioLessonVocab(word.Id, word.Word, word.Reading, word.Meaning)).ToListAsync(ct)
            : [];
        return LyricVocabulary.Resolve(lines, words, requireLinks: true);
    }
}
