using FluentValidation;
using FluentValidation.Results;
using JlptTrainer.Application.Common.Interfaces;
using SkiaSharp;

namespace JlptTrainer.Infrastructure.Media;

public sealed class WebpImageNormalizer : IMediaImageNormalizer
{
    public Stream NormalizeToWebp(Stream source)
    {
        using var input = new SKManagedStream(source, disposeManagedStream: false);
        using var codec = SKCodec.Create(input);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp)
            || codec.Info.Width is < 1 or > 2048 || codec.Info.Height is < 1 or > 2048)
        {
            throw InvalidImage();
        }

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
        {
            throw InvalidImage();
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, 82) ?? throw InvalidImage();
        return new MemoryStream(encoded.ToArray(), writable: false);
    }

    private static ValidationException InvalidImage() =>
        new([new ValidationFailure("File", "Chọn ảnh JPEG, PNG hoặc WebP hợp lệ, tối đa 2048 × 2048 pixel.")]);
}
