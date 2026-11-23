using FluentValidation;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.VideoLessons;

public sealed record PublishVideoLesson(string Title, string? Description, JlptLevel Level, Stream File, long Size) : IRequest<Guid>
{
    public const int MaxSize = 50 * 1024 * 1024;
}

public sealed class PublishVideoLessonValidator : AbstractValidator<PublishVideoLesson>
{
    public PublishVideoLessonValidator()
    {
        RuleFor(item => item.Title).NotEmpty().MaximumLength(150).WithMessage("Tiêu đề tối đa 150 ký tự.");
        RuleFor(item => item.Description).MaximumLength(1000).WithMessage("Mô tả tối đa 1000 ký tự.");
        RuleFor(item => item.Level).IsInEnum().WithMessage("Chọn cấp JLPT từ N5 đến N1.");
        RuleFor(item => item.Size).InclusiveBetween(1, PublishVideoLesson.MaxSize)
            .WithMessage("Video phải có dung lượng từ 1 byte đến 50 MB.");
    }
}

public sealed record ListVideoLessons(JlptLevel? Level = null, int PageNumber = 1, int PageSize = 20) : IRequest<VideoLessonPage>;

public sealed class ListVideoLessonsValidator : AbstractValidator<ListVideoLessons>
{
    public ListVideoLessonsValidator()
    {
        RuleFor(query => query.Level).IsInEnum();
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
    }
}

public sealed record GetVideoLessonPlayback(Guid Id) : IRequest<VideoLessonPlayback>;
public sealed record UnpublishVideoLesson(Guid Id) : IRequest;
public sealed record VideoLessonPlayback(string Url, DateTimeOffset ExpiresAt);
public sealed record VideoLessonItem(Guid Id, string Title, string? Description, JlptLevel Level);
public sealed record VideoLessonPage(IReadOnlyList<VideoLessonItem> Items, int TotalCount, int PageNumber, int PageSize);

public sealed class VideoLessonHandlers(IApplicationDbContext db, IMediaObjectStorage storage, TimeProvider clock)
    : IRequestHandler<PublishVideoLesson, Guid>, IRequestHandler<ListVideoLessons, VideoLessonPage>,
      IRequestHandler<GetVideoLessonPlayback, VideoLessonPlayback>, IRequestHandler<UnpublishVideoLesson>
{
    private static readonly TimeSpan playbackLifetime = TimeSpan.FromMinutes(30);

    public async Task<Guid> Handle(PublishVideoLesson request, CancellationToken ct)
    {
        EnsureStorage();
        Mp4Video.Validate(request.File);
        var id = Guid.NewGuid();
        var lesson = new VideoLesson
        {
            Id = id,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Level = request.Level,
            ObjectKey = $"media/video-lessons/{id:N}.mp4",
            CleanupAfter = clock.GetUtcNow().AddHours(1)
        };
        db.VideoLessons.Add(lesson);
        await db.SaveChangesAsync(ct);
        await storage.UploadAsync(lesson.ObjectKey, request.File, "video/mp4", ct);

        var now = clock.GetUtcNow();
        var published = await db.VideoLessons.Where(row => row.Id == id && row.PublishedAt == null && row.CleanupAfter > now)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.PublishedAt, (DateTimeOffset?)now), ct);
        if (published != 1) throw new ConflictException("Lượt tải video đã hết hạn. Vui lòng thử lại.");
        return id;
    }

    public async Task<VideoLessonPage> Handle(ListVideoLessons request, CancellationToken ct)
    {
        var query = db.VideoLessons.Where(lesson => lesson.PublishedAt != null);
        if (request.Level is { } level) query = query.Where(lesson => lesson.Level == level);
        var count = await query.CountAsync(ct);
        long offset = (request.PageNumber - 1L) * request.PageSize;
        if (offset >= count) return new VideoLessonPage([], count, request.PageNumber, request.PageSize);
        var items = await query.OrderByDescending(lesson => lesson.PublishedAt).ThenBy(lesson => lesson.Id)
            .Skip((int)offset).Take(request.PageSize)
            .Select(lesson => new VideoLessonItem(lesson.Id, lesson.Title, lesson.Description, lesson.Level))
            .ToListAsync(ct);
        return new VideoLessonPage(items, count, request.PageNumber, request.PageSize);
    }

    public async Task<VideoLessonPlayback> Handle(GetVideoLessonPlayback request, CancellationToken ct)
    {
        var expiresAt = clock.GetUtcNow() + playbackLifetime;
        var key = await db.VideoLessons.Where(lesson => lesson.Id == request.Id && lesson.PublishedAt != null)
            .Select(lesson => lesson.ObjectKey).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(VideoLesson), request.Id);
        EnsureStorage();
        return new VideoLessonPlayback(await storage.SignReadAsync(key, expiresAt, ct), expiresAt);
    }

    public async Task Handle(UnpublishVideoLesson request, CancellationToken ct)
    {
        var retireAfter = clock.GetUtcNow() + playbackLifetime;
        var unpublished = await db.VideoLessons.Where(lesson => lesson.Id == request.Id && lesson.PublishedAt != null)
            .ExecuteUpdateAsync(update => update.SetProperty(lesson => lesson.PublishedAt, (DateTimeOffset?)null)
                .SetProperty(lesson => lesson.CleanupAfter, retireAfter), ct);
        if (unpublished == 0) throw new NotFoundException(nameof(VideoLesson), request.Id);
    }

    private void EnsureStorage()
    {
        if (!storage.IsConfigured) throw new ExternalServiceException("S3", "Kho video chưa được cấu hình.");
    }
}
