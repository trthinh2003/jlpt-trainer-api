using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Vocabs.Commands.UpdateVocabContent;

public sealed class UpdateVocabContentCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateVocabContentCommand, Unit>
{
    public async Task<Unit> Handle(UpdateVocabContentCommand request, CancellationToken cancellationToken)
    {
        var meaning = request.Meaning.Trim();
        var example = string.IsNullOrWhiteSpace(request.ExampleSentence) ? null : request.ExampleSentence.Trim();
        var exampleMeaning = string.IsNullOrWhiteSpace(request.ExampleSentenceMeaning)
            ? null
            : request.ExampleSentenceMeaning.Trim();

        var updated = await dbContext.Vocabs
            .Where(vocab => vocab.Id == request.Id)
            .ExecuteUpdateAsync(update => update
                .SetProperty(vocab => vocab.Meaning, meaning)
                .SetProperty(vocab => vocab.ExampleSentence, example)
                .SetProperty(vocab => vocab.ExampleSentenceMeaning, exampleMeaning), cancellationToken);

        if (updated == 0)
        {
            throw new NotFoundException(nameof(Vocab), request.Id);
        }

        return Unit.Value;
    }
}
