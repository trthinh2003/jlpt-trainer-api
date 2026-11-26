namespace JlptTrainer.Application.Common.Interfaces;

public interface IMediaImageNormalizer
{
    Stream NormalizeToWebp(Stream source);
}
