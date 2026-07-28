using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointList;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointById
{
    public sealed class GetGrammarPointByIdQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetGrammarPointByIdQuery, GrammarPointDto>
    {
        public async Task<GrammarPointDto> Handle(GetGrammarPointByIdQuery request, CancellationToken cancellationToken)
        {
            var grammarPoint = await dbContext.GrammarPoints
                .Where(g => g.Id == request.Id)
                .Select(g => new GrammarPointDto(
                    g.Id, g.Pattern, g.Meaning, g.ExampleSentence, g.ExampleSentenceMeaning, g.Level))
                .FirstOrDefaultAsync(cancellationToken);

            return grammarPoint ?? throw new NotFoundException(nameof(GrammarPoint), request.Id);
        }
    }
}
