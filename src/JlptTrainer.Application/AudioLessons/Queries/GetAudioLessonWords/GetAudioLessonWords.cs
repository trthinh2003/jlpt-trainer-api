using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.AudioLessons.Queries.GetAudioLessonWords;

public sealed record GetAudioLessonWords(Guid Id) : IRequest<IReadOnlyList<AudioLessonVocab>>;

public sealed class GetAudioLessonWordsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAudioLessonWords, IReadOnlyList<AudioLessonVocab>>
{
    public async Task<IReadOnlyList<AudioLessonVocab>> Handle(GetAudioLessonWords request, CancellationToken ct)
    {
        var words = await db.PublishedAudioLessons().Where(lesson => lesson.Id == request.Id)
            .Select(lesson => lesson.Words.OrderBy(word => word.Position)
                .Select(word => new AudioLessonVocab(word.VocabId, word.Vocab.Word, word.Vocab.Reading, word.Vocab.Meaning))
                .ToList())
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException(nameof(AudioLesson), request.Id);
        return words;
    }
}
