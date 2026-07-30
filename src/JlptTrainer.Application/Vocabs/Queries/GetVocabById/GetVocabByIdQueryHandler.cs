using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Vocabs.Queries.GetVocabList;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Vocabs.Queries.GetVocabById
{
    public sealed class GetVocabByIdQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetVocabByIdQuery, VocabDto>
    {
        public async Task<VocabDto> Handle(GetVocabByIdQuery request, CancellationToken cancellationToken)
        {
            var vocab = await dbContext.Vocabs
                .Where(v => v.Id == request.Id)
                .Select(v => new VocabDto(
                    v.Id,
                    v.Word,
                    v.Reading,
                    v.Meaning,
                    v.ExampleSentence,
                    v.ExampleSentenceMeaning,
                    v.Level))
                .FirstOrDefaultAsync(cancellationToken);

            return vocab ?? throw new NotFoundException(nameof(Vocab), request.Id);
        }
    }
}
