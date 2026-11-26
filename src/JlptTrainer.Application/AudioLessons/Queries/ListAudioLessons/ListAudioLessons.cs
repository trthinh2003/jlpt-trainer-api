using FluentValidation;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.AudioLessons.Queries.ListAudioLessons;

public sealed record ListAudioLessons(JlptLevel? Level = null, int PageNumber = 1) : IRequest<AudioLessonPage>;
public sealed record AudioLessonItem(Guid Id, string Title, JlptLevel Level);
public sealed record AudioLessonPage(List<AudioLessonItem> Items, int TotalCount, int PageNumber, int PageSize);

public sealed class ListAudioLessonsValidator : AbstractValidator<ListAudioLessons>
{
    public ListAudioLessonsValidator()
    {
        RuleFor(query => query.Level).IsInEnum();
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
    }
}

public sealed class ListAudioLessonsHandler(IApplicationDbContext db)
    : IRequestHandler<ListAudioLessons, AudioLessonPage>
{
    private const int PageSize = 20;

    public async Task<AudioLessonPage> Handle(ListAudioLessons request, CancellationToken ct)
    {
        var query = db.PublishedAudioLessons();
        if (request.Level is { } level) query = query.Where(lesson => lesson.Level == level);
        var count = await query.CountAsync(ct);
        long offset = (request.PageNumber - 1L) * PageSize;
        List<AudioLessonItem> items = offset >= count ? [] : await query
            .OrderByDescending(lesson => lesson.PublishedAt).ThenBy(lesson => lesson.Id)
            .Skip((int)offset).Take(PageSize)
            .Select(lesson => new AudioLessonItem(lesson.Id, lesson.Title, lesson.Level)).ToListAsync(ct);
        return new AudioLessonPage(items, count, request.PageNumber, PageSize);
    }
}
