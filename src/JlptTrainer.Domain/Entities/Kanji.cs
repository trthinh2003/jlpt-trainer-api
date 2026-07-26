using JlptTrainer.Domain.Common;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Domain.Entities
{
    public class Kanji : BaseEntity
    {
        /// ký tự kanji, vd: 食
        public string Character { get; set; } = string.Empty;

        /// âm On (đọc theo Hán), có thể nhiều âm, cách nhau bởi dấu phẩy
        public string OnYomi { get; set; } = string.Empty;

        /// âm Kun (đọc thuần Nhật), có thể nhiều âm, cách nhau bởi dấu phẩy
        public string KunYomi { get; set; } = string.Empty;

        public string Meaning { get; set; } = string.Empty;

        public int StrokeCount { get; set; }

        public JlptLevel Level { get; set; } = JlptLevel.N5;
    }
}
