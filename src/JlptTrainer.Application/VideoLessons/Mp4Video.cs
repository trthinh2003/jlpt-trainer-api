using System.Buffers.Binary;
using FluentValidation;
using FluentValidation.Results;

namespace JlptTrainer.Application.VideoLessons;

internal static class Mp4Video
{
    private const uint FileType = 0x66747970; // ftyp
    private const uint Movie = 0x6D6F6F76; // moov
    private const uint MediaData = 0x6D646174; // mdat
    private const uint Track = 0x7472616B; // trak
    private const uint Media = 0x6D646961; // mdia
    private const uint Handler = 0x68646C72; // hdlr

    public static void Validate(Stream stream)
    {
        var hasFileType = false;
        var hasVideoTrack = false;
        var hasMediaData = false;
        try
        {
            while (ReadBox(stream, stream.Length) is { } box)
            {
                hasFileType |= box.Type == FileType && box.End - stream.Position >= 8;
                hasMediaData |= box.Type == MediaData && box.End > stream.Position;
                if (box.Type == Movie)
                    hasVideoTrack |= HasVideoTrack(stream, box.End);
                stream.Position = box.End;
            }
        }
        catch (EndOfStreamException)
        {
            throw InvalidVideo();
        }

        if (!hasFileType || !hasVideoTrack || !hasMediaData) throw InvalidVideo();
        stream.Position = 0;
    }

    private static bool HasVideoTrack(Stream stream, long end)
    {
        while (ReadBox(stream, end) is { } track)
        {
            if (track.Type == Track && HasVideoHandler(stream, track.End)) return true;
            stream.Position = track.End;
        }
        return false;
    }

    private static bool HasVideoHandler(Stream stream, long trackEnd)
    {
        Span<byte> type = stackalloc byte[4];
        while (ReadBox(stream, trackEnd) is { } media)
        {
            if (media.Type == Media)
            {
                while (ReadBox(stream, media.End) is { } entry)
                {
                    if (entry.Type == Handler && entry.End - stream.Position >= 12)
                    {
                        stream.Position += 8;
                        stream.ReadExactly(type);
                        if (type.SequenceEqual("vide"u8)) return true;
                    }
                    stream.Position = entry.End;
                }
            }
            stream.Position = media.End;
        }
        return false;
    }

    private static (uint Type, long End)? ReadBox(Stream stream, long limit)
    {
        long start = stream.Position;
        if (start == limit) return null;
        if (limit - start < 8) throw InvalidVideo();
        Span<byte> header = stackalloc byte[8];
        stream.ReadExactly(header);
        ulong size = BinaryPrimitives.ReadUInt32BigEndian(header);
        uint type = BinaryPrimitives.ReadUInt32BigEndian(header[4..]);
        if (size == 1)
        {
            if (limit - start < 16) throw InvalidVideo();
            stream.ReadExactly(header);
            size = BinaryPrimitives.ReadUInt64BigEndian(header);
        }
        else if (size == 0)
        {
            size = (ulong)(limit - start);
        }

        if (size < (ulong)(stream.Position - start) || size > (ulong)(limit - start)) throw InvalidVideo();
        return (type, start + (long)size);
    }

    private static ValidationException InvalidVideo() =>
        new([new ValidationFailure("File", "Chọn file MP4 hợp lệ có chứa video.")]);
}
