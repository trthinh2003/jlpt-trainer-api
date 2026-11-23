using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Common;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JlptTrainer.Infrastructure.Media;

public sealed class MediaCleanup(IServiceScopeFactory scopes, IMediaObjectStorage storage,
    TimeProvider clock, ILogger<MediaCleanup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!storage.IsConfigured) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                for (var i = 0; i < 20; i++)
                {
                    var now = clock.GetUtcNow();
                    var image = await DeleteOneAsync<VocabImage>($"""
                        SELECT * FROM vocab_images WHERE vocab_id IS NULL AND cleanup_after <= {now}
                        ORDER BY cleanup_after LIMIT 1 FOR UPDATE SKIP LOCKED
                        """, stoppingToken);
                    var video = await DeleteOneAsync<VideoLesson>($"""
                        SELECT * FROM video_lessons WHERE published_at IS NULL AND cleanup_after <= {now}
                        ORDER BY cleanup_after LIMIT 1 FOR UPDATE SKIP LOCKED
                        """, stoppingToken);
                    var audio = await DeleteOneAsync<AudioLesson>($"""
                        SELECT * FROM audio_lessons WHERE published_at IS NULL AND cleanup_after <= {now}
                        ORDER BY cleanup_after LIMIT 1 FOR UPDATE SKIP LOCKED
                        """, stoppingToken, lesson => lesson.CoverKey);
                    if (!image && !video && !audio) break;
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Could not process pending media objects.");
            }
        }
    }

    private async Task<bool> DeleteOneAsync<T>(FormattableString sql, CancellationToken ct, Func<T, string>? otherKey = null)
        where T : class, IStoredMediaObject
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var result = await db.Set<T>().FromSql(sql).ToListAsync(ct);
        if (result.Count == 0) return false;

        var media = result[0];
        try
        {
            await storage.DeleteAsync(media.ObjectKey, ct);
            if (otherKey is not null) await storage.DeleteAsync(otherKey(media), ct);
            db.Remove(media);
        }
        catch (ExternalServiceException)
        {
            media.CleanupAfter = clock.GetUtcNow().AddMinutes(5);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }
}
