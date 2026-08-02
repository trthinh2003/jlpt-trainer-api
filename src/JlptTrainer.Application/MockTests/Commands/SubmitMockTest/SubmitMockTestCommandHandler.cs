using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.MockTests.Commands.SubmitMockTest
{
    public sealed class SubmitMockTestCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        TimeProvider timeProvider
    ) : IRequestHandler<SubmitMockTestCommand, SubmitMockTestResult>
    {
        public async Task<SubmitMockTestResult> Handle(SubmitMockTestCommand request, CancellationToken cancellationToken)
        {
            var vocabIds = request.Answers.Select(a => a.VocabId).ToList();

            var correctMeanings = await dbContext.Vocabs
                .Where(v => vocabIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.Meaning, cancellationToken);

            var correctCount = request.Answers.Count(answer =>
                correctMeanings.TryGetValue(answer.VocabId, out var actualMeaning)
                && string.Equals(actualMeaning.Trim(), answer.SelectedMeaning.Trim(), StringComparison.OrdinalIgnoreCase));

            var mockTest = new MockTest
            {
                UserId = currentUser.UserId,
                Level = request.Level,
                TotalQuestions = request.Answers.Count,
                CorrectAnswers = correctCount,
                Duration = request.Duration,
                TakenAt = timeProvider.GetUtcNow()
            };

            dbContext.MockTests.Add(mockTest);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new SubmitMockTestResult(
                mockTest.Id,
                mockTest.TotalQuestions,
                mockTest.CorrectAnswers,
                mockTest.ScorePercentage);
        }
    }
}
