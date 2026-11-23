using FluentValidation;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Common.Media;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Vocabs.Images;

public sealed record UploadVocabImage(Guid Id, Stream File, long Size) : IRequest
{
    public const int MaxSize = 5 * 1024 * 1024;
}

public sealed class UploadVocabImageValidator : AbstractValidator<UploadVocabImage>
{
    public UploadVocabImageValidator() =>
        RuleFor(request => request.Size).InclusiveBetween(1, UploadVocabImage.MaxSize)
            .WithMessage("Ảnh phải có dung lượng từ 1 byte đến 5 MB.");
}

public sealed record RemoveVocabImage(Guid Id) : IRequest;
public sealed record GetVocabImage(Guid Id) : IRequest<VocabImageLink?>;
public sealed record VocabImageLink(string Url);

public sealed class VocabImageHandlers(
    IApplicationDbContext db, IVocabImageStore images, IMediaObjectStorage storage, TimeProvider clock)
    : IRequestHandler<UploadVocabImage>, IRequestHandler<RemoveVocabImage>, IRequestHandler<GetVocabImage, VocabImageLink?>
{
    private static readonly TimeSpan readLifetime = TimeSpan.FromMinutes(5);

    public async Task Handle(UploadVocabImage request, CancellationToken ct)
    {
        if (!storage.IsConfigured) throw new ExternalServiceException("S3", "Kho ảnh chưa được cấu hình.");
        if (!await db.Vocabs.AnyAsync(vocab => vocab.Id == request.Id, ct))
            throw new NotFoundException(nameof(Vocab), request.Id);

        using var normalized = ImageFile.Normalize(request.File);
        var key = $"media/vocab-images/{request.Id:N}/{Guid.NewGuid():N}.webp";
        await images.StageAsync(key, clock.GetUtcNow().AddHours(1), ct);
        await storage.UploadAsync(key, normalized, "image/webp", ct);
        await images.SetCurrentAsync(request.Id, key, clock.GetUtcNow() + readLifetime, ct);
    }

    public Task Handle(RemoveVocabImage request, CancellationToken ct) =>
        images.SetCurrentAsync(request.Id, null, clock.GetUtcNow() + readLifetime, ct);

    public async Task<VocabImageLink?> Handle(GetVocabImage request, CancellationToken ct)
    {
        var vocab = await db.Vocabs.Where(item => item.Id == request.Id)
            .Select(item => new { Key = item.Image == null ? null : item.Image.ObjectKey })
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(Vocab), request.Id);

        if (vocab.Key is null || !storage.IsConfigured) return null;
        return new VocabImageLink(await storage.SignReadAsync(vocab.Key, clock.GetUtcNow() + readLifetime, ct));
    }
}
