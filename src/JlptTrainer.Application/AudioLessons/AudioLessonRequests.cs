using FluentValidation;
using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.AudioLessons;

public sealed record PublishAudioLesson(string Title, JlptLevel Level, string Lyrics, Guid[] VocabIds,
    Func<Stream> OpenAudio, long AudioSize, Func<Stream> OpenCover, long CoverSize) : IRequest<Guid>
{
    public const int MaxAudioBytes = 25 * 1024 * 1024;
    public const int MaxCoverBytes = 5 * 1024 * 1024;
}

public sealed class PublishAudioLessonValidator : AbstractValidator<PublishAudioLesson>
{
    public PublishAudioLessonValidator()
    {
        RuleFor(request => request.Title).NotEmpty().WithMessage("Nhập tiêu đề bài nghe.")
            .MaximumLength(150).WithMessage("Tiêu đề tối đa 150 ký tự.");
        RuleFor(request => request.Level).IsInEnum();
        RuleFor(request => request.Lyrics).WithLrcTextLimits();
        RuleFor(request => request.VocabIds).Must(ids => ids.Length is >= 1 and <= 20 && ids.Distinct().Count() == ids.Length)
            .WithMessage("Chọn từ 1 đến 20 mục từ vựng khác nhau.");
        RuleFor(request => request.AudioSize).InclusiveBetween(1, PublishAudioLesson.MaxAudioBytes)
            .WithMessage("File âm thanh tối đa 25 MB.");
        RuleFor(request => request.CoverSize).InclusiveBetween(1, PublishAudioLesson.MaxCoverBytes)
            .WithMessage("Ảnh bìa tối đa 5 MB.");
    }
}

public sealed record ListAudioLessons(JlptLevel? Level = null, int PageNumber = 1) : IRequest<AudioLessonPage>;
public sealed record GetAudioLesson(Guid Id) : IRequest<AudioLessonPlayback>;
public sealed record GetAudioLessonWords(Guid Id) : IRequest<IReadOnlyList<AudioLessonVocab>>;
public sealed record UnpublishAudioLesson(Guid Id) : IRequest;

public sealed class ListAudioLessonsValidator : AbstractValidator<ListAudioLessons>
{
    public ListAudioLessonsValidator()
    {
        RuleFor(query => query.Level).IsInEnum();
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
    }
}

public sealed record AudioLessonItem(Guid Id, string Title, JlptLevel Level);
public sealed record AudioLessonPage(List<AudioLessonItem> Items, int TotalCount, int PageNumber, int PageSize);
public sealed record AudioLessonPlayback(string AudioUrl, string CoverUrl, LyricLine[] Lyrics);
public sealed record AudioLessonVocab(Guid Id, string Word, string Reading, string Meaning);
