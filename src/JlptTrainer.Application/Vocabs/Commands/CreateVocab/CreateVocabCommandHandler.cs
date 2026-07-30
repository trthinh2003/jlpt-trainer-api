using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;

namespace JlptTrainer.Application.Vocabs.Commands.CreateVocab
{
    public sealed class CreateVocabCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateVocabCommand, Guid>
    {
        public async Task<Guid> Handle(CreateVocabCommand request, CancellationToken cancellationToken)
        {
            var vocab = new Vocab
            {
                Word = request.Word.Trim(),
                Reading = request.Reading.Trim(),
                Meaning = request.Meaning.Trim(),
                ExampleSentence = request.ExampleSentence?.Trim(),
                ExampleSentenceMeaning = request.ExampleSentenceMeaning?.Trim(),
                Level = request.Level
            };

            dbContext.Vocabs.Add(vocab);
            await dbContext.SaveChangesAsync(cancellationToken);

            return vocab.Id;
        }
    }
}
