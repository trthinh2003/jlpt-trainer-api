using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;

namespace JlptTrainer.Application.AudioLessons;

internal static class AudioLessonQueries
{
    public static IQueryable<AudioLesson> PublishedAudioLessons(this IApplicationDbContext db) =>
        db.AudioLessons.Where(lesson => lesson.PublishedAt != null);
}
