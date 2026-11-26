using JlptTrainer.Application.AudioLessons.Commands.PublishAudioLesson;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace JlptTrainer.Infrastructure.Persistence;

internal sealed class AudioLessonPublicationConflictInterceptor : SaveChangesInterceptor
{
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Exception is DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: PostgresErrorCodes.ForeignKeyViolation,
                    ConstraintName: AudioLessonWordConfiguration.VocabForeignKeyName
                }
            })
        {
            throw new ConflictException(PublishAudioLesson.MissingVocabMessage);
        }

        return Task.CompletedTask;
    }
}
