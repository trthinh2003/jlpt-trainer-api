using FluentValidation;
using JlptTrainer.Application.AudioLessons.Lyrics;
using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.AudioLessons.Commands.PublishAudioLesson;

public sealed record PublishAudioLesson(string Title, JlptLevel Level, string Lyrics, Guid[] VocabIds,
    Func<Stream> OpenAudio, long AudioSize, Func<Stream> OpenCover, long CoverSize) : IRequest<Guid>
{
    public const int MaxAudioBytes = 25 * 1024 * 1024;
    public const int MaxCoverBytes = 5 * 1024 * 1024;
    public const string MissingVocabMessage = "Có từ đã bị gỡ khỏi kho. Vui lòng chọn lại.";
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
