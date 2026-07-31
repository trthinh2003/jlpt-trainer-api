using MediatR;

namespace JlptTrainer.Application.Kanjis.Commands.ImportKanjiFromExcel
{
    public sealed record ImportKanjiFromExcelCommand(byte[] FileContent) : IRequest<ImportKanjiResult>;
}
