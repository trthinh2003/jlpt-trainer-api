using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointImportTemplate
{
    public sealed record GetGrammarPointImportTemplateQuery : IRequest<byte[]>;
}
