using JlptTrainer.Domain.Common;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Domain.Entities
{
    public class GrammarPoint : BaseEntity
    {
        /// mẫu ngữ pháp, vd: 〜てください
        public string Pattern { get; set; } = string.Empty;

        public string Meaning { get; set; } = string.Empty;

        public string? ExampleSentence { get; set; }

        public string? ExampleSentenceMeaning { get; set; }

        public JlptLevel Level { get; set; } = JlptLevel.N5;
    }
}
