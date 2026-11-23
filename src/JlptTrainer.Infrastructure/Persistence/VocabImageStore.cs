using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Vocabs.Images;
using JlptTrainer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Infrastructure.Persistence;

public sealed class VocabImageStore(ApplicationDbContext db, TimeProvider clock) : IVocabImageStore
{
    public async Task StageAsync(string key, DateTimeOffset cleanupAfter, CancellationToken ct)
    {
        db.Set<VocabImage>().Add(new VocabImage { ObjectKey = key, CleanupAfter = cleanupAfter });
        await db.SaveChangesAsync(ct);
    }

    public async Task SetCurrentAsync(Guid id, string? key, DateTimeOffset retireAfter, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var locked = await db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM vocabs WHERE id = {id} FOR UPDATE")
            .ToListAsync(ct);
        if (locked.Count == 0) throw new NotFoundException(nameof(Vocab), id);

        var images = db.Set<VocabImage>();
        await images.Where(image => image.VocabId == id)
            .ExecuteUpdateAsync(update => update.SetProperty(image => image.VocabId, (Guid?)null)
                .SetProperty(image => image.CleanupAfter, retireAfter), ct);

        if (key is not null)
        {
            var now = clock.GetUtcNow();
            var updated = await images.Where(image => image.ObjectKey == key && image.VocabId == null && image.CleanupAfter > now)
                .ExecuteUpdateAsync(update => update.SetProperty(image => image.VocabId, (Guid?)id), ct);
            if (updated != 1) throw new ConflictException("Lượt tải ảnh đã hết hạn. Vui lòng thử lại.");
        }

        await transaction.CommitAsync(ct);
    }
}
