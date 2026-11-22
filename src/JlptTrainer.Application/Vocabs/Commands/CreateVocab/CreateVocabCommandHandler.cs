using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Domain.Entities;
using MediatR;

namespace JlptTrainer.Application.Vocabs.Commands.CreateVocab
{
    public sealed class CreateVocabCommandHandler(IVocabCatalogWriter writer) : IRequestHandler<CreateVocabCommand, Guid>
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

            if (await writer.InsertMissingAsync([vocab], cancellationToken) == 0)
            {
                throw new ConflictException("Từ và cách đọc này đã có trong kho từ vựng.");
            }

            return vocab.Id;
        }
    }
}
