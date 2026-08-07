using System.Text.Json.Serialization;

namespace JlptTrainer.Infrastructure.ExternalServices.Jisho
{
    internal sealed class JishoApiResponse
    {
        [JsonPropertyName("data")]
        public List<JishoDataItem> Data { get; set; } = [];
    }

    internal sealed class JishoDataItem
    {
        [JsonPropertyName("is_common")]
        public bool? IsCommon { get; set; }

        [JsonPropertyName("japanese")]
        public List<JishoJapanese> Japanese { get; set; } = [];

        [JsonPropertyName("senses")]
        public List<JishoSense> Senses { get; set; } = [];
    }

    internal sealed class JishoJapanese
    {
        [JsonPropertyName("word")]
        public string? Word { get; set; } // word có thể null với từ chỉ viết bằng kana (không có dạng kanji riêng)

        [JsonPropertyName("reading")]
        public string? Reading { get; set; }
    }

    internal sealed class JishoSense
    {
        [JsonPropertyName("english_definitions")]
        public List<string> EnglishDefinitions { get; set; } = [];
    }
}
