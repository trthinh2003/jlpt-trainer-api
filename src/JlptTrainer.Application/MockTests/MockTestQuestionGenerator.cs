using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.MockTests;

internal static class MockTestQuestionGenerator
{
    private const int ChoicesPerQuestion = 4;

    internal sealed record Question(Guid VocabId, string Word, string Reading, string[] Choices, int CorrectChoiceIndex);

    public static async Task<List<Question>> GenerateAsync(
        IApplicationDbContext dbContext, JlptLevel level, int count, CancellationToken cancellationToken)
    {
        var pool = await dbContext.Vocabs
            .Where(vocab => vocab.Level == level)
            .Select(vocab => new { vocab.Id, vocab.Word, vocab.Reading, vocab.Meaning })
            .ToArrayAsync(cancellationToken);
        var comparer = StringComparer.OrdinalIgnoreCase;
        var meanings = pool.Select(vocab => vocab.Meaning.Trim()).Distinct(comparer).ToArray();
        if (meanings.Length < ChoicesPerQuestion)
        {
            throw new InsufficientDataException($"Cần ít nhất {ChoicesPerQuestion} nghĩa khác nhau ở cấp {level} để tạo đề.");
        }

        Random.Shared.Shuffle(pool.AsSpan());
        var questions = new List<Question>(Math.Min(count, pool.Length));
        foreach (var vocab in pool.Take(count))
        {
            var correct = vocab.Meaning.Trim();
            var options = new HashSet<string>(comparer) { correct };
            while (options.Count < ChoicesPerQuestion)
            {
                options.Add(meanings[Random.Shared.Next(meanings.Length)]);
            }
            var choices = options.ToArray();
            Random.Shared.Shuffle(choices.AsSpan());
            questions.Add(new Question(vocab.Id, vocab.Word, vocab.Reading, choices, Array.IndexOf(choices, correct)));
        }
        return questions;
    }
}
