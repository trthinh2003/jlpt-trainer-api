using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.MockTests;

public sealed record MockTestAttemptQuestionDto(string Word, string Reading, string[] Choices);

public sealed record MockTestAttemptDto(
    Guid Id, JlptLevel Level, DateTimeOffset ExpiresAt, DateTimeOffset ServerTime,
    Guid? ResultId, IReadOnlyList<MockTestAttemptQuestionDto> Questions)
{
    internal static MockTestAttemptDto Create(MockTestAttempt attempt, DateTimeOffset serverTime, Guid? resultId = null) =>
        new(attempt.Id, attempt.Level, attempt.ExpiresAt, serverTime, resultId, attempt.Questions
            .OrderBy(question => question.Position)
            .Select(question => new MockTestAttemptQuestionDto(question.Word, question.Reading, question.Choices)).ToArray());
}
