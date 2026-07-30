using MediatR;

namespace JlptTrainer.Application.Vocabs.ImportVocabFromExcel
{
    public sealed record ImportVocabFromExcelCommand(byte[] FileContent) : IRequest<ImportVocabResult>;
}
