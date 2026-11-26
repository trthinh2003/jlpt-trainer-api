using FluentValidation;
using FluentValidation.Results;
using System.Globalization;
using System.Text.RegularExpressions;

namespace JlptTrainer.Application.AudioLessons.Lyrics;

internal static partial class LrcLyrics
{
    public static IRuleBuilderOptions<T, string> WithLrcTextLimits<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Nhập lời có mốc thời gian.")
            .MaximumLength(28000).WithMessage("Lời tối đa 28.000 ký tự.");

    [GeneratedRegex(@"\[(?<min>[0-9]{1,3}):(?<sec>[0-5][0-9])(?:[.:](?<part>[0-9]{1,3}))?\]", RegexOptions.CultureInvariant)]
    private static partial Regex Timestamp();

    [GeneratedRegex(@"^\[(?:ar|ti|al|au|by|re|ve|length):[^\]]*\]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Metadata();

    public static LyricLine[] Parse(string source)
    {
        var lines = new List<LyricLine>();
        var offset = 0;
        foreach (var raw in source.TrimStart('\uFEFF').Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || Metadata().IsMatch(line)) continue;
            if (line.StartsWith("[offset:", StringComparison.OrdinalIgnoreCase) && line.EndsWith(']'))
            {
                if (!int.TryParse(line.AsSpan(8, line.Length - 9), NumberStyles.Integer, CultureInfo.InvariantCulture, out offset)
                    || offset is < -60000 or > 60000)
                    throw InvalidLyrics();
                continue;
            }

            var matches = Timestamp().Matches(line);
            var end = 0;
            foreach (Match match in matches)
            {
                if (match.Index != end) break;
                end += match.Length;
            }
            if (end == 0) throw InvalidLyrics();
            var text = line[end..].Trim();
            foreach (Match match in matches)
            {
                if (match.Index >= end) break;
                var part = match.Groups["part"].Value.PadRight(3, '0');
                var ms = int.Parse(match.Groups["min"].Value, CultureInfo.InvariantCulture) * 60_000
                    + int.Parse(match.Groups["sec"].Value, CultureInfo.InvariantCulture) * 1000
                    + int.Parse(part, CultureInfo.InvariantCulture);
                lines.Add(new LyricLine(ms, text));
                if (lines.Count > 400) throw InvalidLyrics();
            }
        }
        if (!lines.Any(line => line.Text.Length > 0)) throw InvalidLyrics();
        return lines.GroupBy(line => Math.Max(0, line.StartMs + offset))
            .OrderBy(group => group.Key)
            .Select(group => new LyricLine(group.Key, string.Join('\n', group.Select(line => line.Text))))
            .ToArray();
    }

    private static ValidationException InvalidLyrics() =>
        new([new ValidationFailure("Lyrics", "Lời cần có mốc LRC hợp lệ, ví dụ [00:05.20]おはよう。 Tối đa 400 mốc.")]);
}
