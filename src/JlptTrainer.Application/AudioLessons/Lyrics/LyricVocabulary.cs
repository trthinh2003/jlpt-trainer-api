using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;

namespace JlptTrainer.Application.AudioLessons.Lyrics;

internal static partial class LyricVocabulary
{
    // An explicit catalog ID keeps the link stable when the word is edited.
    [GeneratedRegex(@"\[\[(?<text>[^\[\]|\r\n]+)\|(?<id>[0-9a-fA-F]{32})\]\]", RegexOptions.CultureInvariant)]
    private static partial Regex Marker();

    public static LyricLine[] Resolve(LyricLine[] lines, IEnumerable<AudioLessonVocab> words, bool requireLinks = false)
    {
        var byId = words.ToDictionary(word => word.Id);
        return lines.Select(line => ResolveLine(line, byId, requireLinks)).ToArray();
    }

    private static LyricLine ResolveLine(LyricLine line, Dictionary<Guid, AudioLessonVocab> byId, bool requireLinks)
    {
        var matches = Marker().Matches(line.Text);
        if (matches.Count == 0) return line;

        var text = new StringBuilder();
        var links = new List<LyricWordLink>();
        var next = 0;
        var firstLineEnd = line.Text.IndexOf('\n');
        foreach (Match match in matches)
        {
            text.Append(line.Text.AsSpan(next, match.Index - next));
            var surface = match.Groups["text"].Value;
            var id = Guid.ParseExact(match.Groups["id"].ValueSpan, "N");
            var onJapaneseLine = firstLineEnd < 0 || match.Index < firstLineEnd;
            if (onJapaneseLine && byId.TryGetValue(id, out var word))
                links.Add(new LyricWordLink(text.Length, surface.Length, word));
            else if (requireLinks)
                throw new ValidationException([new ValidationFailure("Lyrics",
                    onJapaneseLine
                        ? $"Từ đánh dấu “{surface}” cần nằm trong danh sách từ đã chọn."
                        : "Chỉ đánh dấu từ trên dòng lời Nhật ở đầu mỗi mốc.")]);

            text.Append(surface);
            next = match.Index + match.Length;
        }
        text.Append(line.Text.AsSpan(next));
        return line with { Text = text.ToString(), Links = links.Count > 0 ? links.ToArray() : null };
    }
}
