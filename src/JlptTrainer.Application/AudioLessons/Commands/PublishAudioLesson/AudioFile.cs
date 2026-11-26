using System.Buffers.Binary;
using FluentValidation;
using FluentValidation.Results;

namespace JlptTrainer.Application.AudioLessons.Commands.PublishAudioLesson;

internal static class AudioFile
{
    public static (string Extension, string ContentType) Inspect(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        if (stream.Length < header.Length) throw InvalidAudio();
        stream.ReadExactly(header);

        if (header[..4].SequenceEqual("RIFF"u8) && header[8..].SequenceEqual("WAVE"u8) && HasWaveData(stream, header))
        {
            stream.Position = 0;
            return ("wav", "audio/wav");
        }

        var start = 0L;
        if (header[..3].SequenceEqual("ID3"u8) && header[6..10].IndexOfAnyInRange((byte)128, byte.MaxValue) < 0)
        {
            start = 10L + (header[6] << 21) + (header[7] << 14) + (header[8] << 7) + header[9];
            if ((header[5] & 0x10) != 0) start += 10;
        }

        if (start <= stream.Length - 4)
        {
            stream.Position = start;
            stream.ReadExactly(header[..4]);
            if (header[0] == 0xff && (header[1] & 0xe0) == 0xe0 && (header[1] & 6) == 2
                && (header[1] & 0x18) != 8 && (header[2] >> 4) is > 0 and < 15 && (header[2] & 12) != 12)
            {
                stream.Position = 0;
                return ("mp3", "audio/mpeg");
            }
        }

        throw InvalidAudio();
    }

    private static bool HasWaveData(Stream stream, ReadOnlySpan<byte> header)
    {
        long end = 8L + BinaryPrimitives.ReadUInt32LittleEndian(header[4..]);
        if (end > stream.Length) return false;
        var hasFormat = false;
        Span<byte> chunk = stackalloc byte[8];
        while (stream.Position + chunk.Length <= end)
        {
            stream.ReadExactly(chunk);
            var length = BinaryPrimitives.ReadUInt32LittleEndian(chunk[4..]);
            long next = stream.Position + length + (length & 1);
            if (next > end) return false;
            if (chunk[..4].SequenceEqual("fmt "u8)) hasFormat |= length >= 16;
            if (chunk[..4].SequenceEqual("data"u8) && hasFormat && length > 0) return true;
            stream.Position = next;
        }
        return false;
    }

    private static ValidationException InvalidAudio() =>
        new([new ValidationFailure("Audio", "Chọn file âm thanh MP3 hoặc WAV hợp lệ.")]);
}
