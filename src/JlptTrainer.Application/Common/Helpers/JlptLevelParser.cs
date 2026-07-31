using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.Common.Helpers
{
    public static class JlptLevelParser
    {
        public static bool TryParse(string? raw, out JlptLevel level)
        {
            level = JlptLevel.N5;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return true; 
            }

            var trimmed = raw.Trim().ToUpperInvariant();

            if (trimmed.StartsWith('N') && Enum.TryParse(trimmed, out level))
            {
                return true;
            }

            if (int.TryParse(trimmed, out var num) && Enum.IsDefined(typeof(JlptLevel), num))
            {
                level = (JlptLevel)num;
                return true;
            }

            return false;
        }
    }
}
