using MediatR;

namespace JlptTrainer.Application.Vocabs.Queries.GetVocabImportTemplate
{
    public sealed record GetVocabImportTemplateQuery : IRequest<byte[]>;
}
