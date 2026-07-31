using MediatR;

namespace JlptTrainer.Application.Kanjis.Queries.GetKanjiImportTemplate
{
    public sealed record GetKanjiImportTemplateQuery : IRequest<byte[]>;
}
