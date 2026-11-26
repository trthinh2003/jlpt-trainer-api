using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;

namespace JlptTrainer.Application.AudioLessons;

internal static class AudioLessonMedia
{
    public static readonly TimeSpan PlaybackLifetime = TimeSpan.FromMinutes(30);

    public static void RequireConfigured(IMediaObjectStorage storage)
    {
        if (!storage.IsConfigured) throw new ExternalServiceException("S3", "Kho audio chưa được cấu hình.");
    }
}
