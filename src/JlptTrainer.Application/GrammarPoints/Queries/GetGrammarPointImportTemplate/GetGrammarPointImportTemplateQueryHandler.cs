using JlptTrainer.Application.Common.Interfaces;
using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointImportTemplate
{
    public sealed class GetGrammarPointImportTemplateQueryHandler(IExcelTemplateGenerator templateGenerator) : IRequestHandler<GetGrammarPointImportTemplateQuery, byte[]>
    {
        private static readonly string[] Headers = ["Pattern", "Meaning", "ExampleSentence", "ExampleSentenceMeaning", "Level"];

        private static readonly string[][] SampleRows =
        [
            ["〜てください", "Làm ơn hãy...", "食べてください", "Hãy ăn đi", "N5"],
            ["〜たいです", "Muốn làm...", "水が飲みたいです", "Tôi muốn uống nước", "N5"]
        ];

        public Task<byte[]> Handle(GetGrammarPointImportTemplateQuery request, CancellationToken cancellationToken)
        {
            var bytes = templateGenerator.Generate("GrammarPoint", Headers, SampleRows);
            return Task.FromResult(bytes);
        }
    }
}
