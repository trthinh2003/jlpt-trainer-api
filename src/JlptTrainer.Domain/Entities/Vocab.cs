using JlptTrainer.Domain.Common;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Domain.Entities
{
    public class Vocab : BaseEntity
    {
        public string Word { get; set; } = string.Empty;
        public string Reading { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string? ExampleSentence { get; set; }
        public string? ExampleSentenceMeaning { get; set; }
        public JlptLevel Level { get; set; } = JlptLevel.N5;
    }
}
