using JlptTrainer.Application.Common.Interfaces;
using MediatR;

namespace JlptTrainer.Application.Kanjis.Queries.GetKanjiImportTemplate
{
    public sealed class GetKanjiImportTemplateQueryHandler(IExcelTemplateGenerator templateGenerator) : IRequestHandler<GetKanjiImportTemplateQuery, byte[]>
    {
        private static readonly string[] Headers = ["Character", "OnYomi", "KunYomi", "Meaning", "StrokeCount", "Level"];

        private static readonly string[][] SampleRows =
        [
            ["食", "ショク", "た.べる", "ăn, thức ăn", "9", "N5"],
            ["水", "スイ", "みず", "nước", "4", "N5"]
        ];

        public Task<byte[]> Handle(GetKanjiImportTemplateQuery request, CancellationToken cancellationToken)
        {
            var bytes = templateGenerator.Generate("Kanji", Headers, SampleRows);
            return Task.FromResult(bytes);
        }
    }
}
