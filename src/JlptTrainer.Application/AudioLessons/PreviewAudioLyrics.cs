using FluentValidation;
using MediatR;

namespace JlptTrainer.Application.AudioLessons;

public sealed record PreviewAudioLyrics(string Lyrics) : IRequest<LyricLine[]>;

public sealed class PreviewAudioLyricsValidator : AbstractValidator<PreviewAudioLyrics>
{
    public PreviewAudioLyricsValidator() => RuleFor(request => request.Lyrics).WithLrcTextLimits();
}

public sealed class PreviewAudioLyricsHandler : IRequestHandler<PreviewAudioLyrics, LyricLine[]>
{
    public Task<LyricLine[]> Handle(PreviewAudioLyrics request, CancellationToken ct) =>
        Task.FromResult(LrcLyrics.Parse(request.Lyrics));
}
