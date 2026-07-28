using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Commands.CreateGrammarPoint
{
    public sealed class CreateGrammarPointCommandHandler(IApplicationDbContext dbContext)
        : IRequestHandler<CreateGrammarPointCommand, Guid>
    {
        public async Task<Guid> Handle(CreateGrammarPointCommand request, CancellationToken cancellationToken)
        {
            var grammarPoint = new GrammarPoint
            {
                Pattern = request.Pattern.Trim(),
                Meaning = request.Meaning.Trim(),
                ExampleSentence = request.ExampleSentence?.Trim(),
                ExampleSentenceMeaning = request.ExampleSentenceMeaning?.Trim(),
                Level = request.Level
            };

            dbContext.GrammarPoints.Add(grammarPoint);
            await dbContext.SaveChangesAsync(cancellationToken);

            return grammarPoint.Id;
        }
    }
}
