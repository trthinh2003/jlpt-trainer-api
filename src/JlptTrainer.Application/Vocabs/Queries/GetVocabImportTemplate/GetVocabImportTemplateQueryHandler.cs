using JlptTrainer.Application.Common.Interfaces;
using MediatR;

namespace JlptTrainer.Application.Vocabs.Queries.GetVocabImportTemplate
{
    public sealed class GetVocabImportTemplateQueryHandler(IExcelTemplateGenerator templateGenerator) : IRequestHandler<GetVocabImportTemplateQuery, byte[]>
    {
        private static readonly string[] Headers = ["Word", "Reading", "Meaning", "ExampleSentence", "ExampleSentenceMeaning", "Level"];

        private static readonly string[][] SampleRows =
        [
            ["食べる", "たべる", "ăn", "朝ご飯を食べる", "Ăn bữa sáng", "N5"],
            ["飲む", "のむ", "uống", "水を飲む", "Uống nước", "5"]
        ];

        public Task<byte[]> Handle(GetVocabImportTemplateQuery request, CancellationToken cancellationToken)
        {
            var bytes = templateGenerator.Generate("Vocab", Headers, SampleRows);
            return Task.FromResult(bytes);
        }
    }
}
