using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.GrammarPoints.Commands.UpdateGrammarPointContent;

public sealed class UpdateGrammarPointContentCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateGrammarPointContentCommand, Unit>
{
    public async Task<Unit> Handle(UpdateGrammarPointContentCommand request, CancellationToken cancellationToken)
    {
        var meaning = request.Meaning.Trim();
        var exampleSentence = string.IsNullOrWhiteSpace(request.ExampleSentence) ? null : request.ExampleSentence.Trim();
        var exampleSentenceMeaning = string.IsNullOrWhiteSpace(request.ExampleSentenceMeaning) ? null : request.ExampleSentenceMeaning.Trim();

        var updated = await dbContext.GrammarPoints
            .Where(item => item.Id == request.Id)
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.Meaning, meaning)
                .SetProperty(item => item.ExampleSentence, exampleSentence)
                .SetProperty(item => item.ExampleSentenceMeaning, exampleSentenceMeaning), cancellationToken);

        if (updated == 0)
        {
            throw new NotFoundException(nameof(GrammarPoint), request.Id);
        }

        return Unit.Value;
    }
}
