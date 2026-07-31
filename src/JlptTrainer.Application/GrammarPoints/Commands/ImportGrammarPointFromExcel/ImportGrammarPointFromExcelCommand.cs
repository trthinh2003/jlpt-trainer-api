using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Commands.ImportGrammarPointFromExcel
{
    public sealed record ImportGrammarPointFromExcelCommand(byte[] FileContent) : IRequest<ImportGrammarPointResult>;
}
