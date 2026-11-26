using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.AudioLessons.Commands.UnpublishAudioLesson;

public sealed record UnpublishAudioLesson(Guid Id) : IRequest;

public sealed class UnpublishAudioLessonHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<UnpublishAudioLesson>
{
    public async Task Handle(UnpublishAudioLesson request, CancellationToken ct)
    {
        var retireAfter = clock.GetUtcNow() + AudioLessonMedia.PlaybackLifetime;
        var changed = await db.PublishedAudioLessons().Where(lesson => lesson.Id == request.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(lesson => lesson.PublishedAt, (DateTimeOffset?)null)
                .SetProperty(lesson => lesson.CleanupAfter, retireAfter), ct);
        if (changed == 0) throw new NotFoundException(nameof(AudioLesson), request.Id);
    }
}
