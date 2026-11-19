using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestReview;

public sealed record GetMockTestReviewQuery(Guid ResultId) : IRequest<MockTestReviewDto>;

public sealed record MockTestReviewQuestionDto(
    string Word, string Reading, string[] Choices, int? SelectedChoiceIndex, int CorrectChoiceIndex, Guid? ReviewVocabId);

public sealed record MockTestReviewDto(
    Guid Id, JlptLevel Level, int TotalQuestions, int CorrectAnswers, double ScorePercentage,
    TimeSpan Duration, DateTimeOffset TakenAt, IReadOnlyList<MockTestReviewQuestionDto>? Questions);

public sealed class GetMockTestReviewQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<GetMockTestReviewQuery, MockTestReviewDto>
{
    public async Task<MockTestReviewDto> Handle(GetMockTestReviewQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var result = await dbContext.MockTests.AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == request.ResultId && entry.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(MockTest), request.ResultId);

        List<MockTestReviewQuestionDto>? questions = null;
        if (result.AttemptId is Guid attemptId && result.SelectedChoices is { } selections)
        {
            var attempt = await dbContext.MockTestAttempts.AsNoTracking().Include(entry => entry.Questions)
                .SingleAsync(entry => entry.Id == attemptId && entry.UserId == userId, cancellationToken);
            var missedVocabIds = attempt.Questions
                .Where(question => selections[question.Position] != question.CorrectChoiceIndex)
                .Select(question => question.SourceVocabId)
                .OfType<Guid>()
                .ToArray();
            List<Guid> availableVocabIds = missedVocabIds.Length == 0
                ? []
                : await dbContext.Vocabs.Where(vocab => missedVocabIds.Contains(vocab.Id))
                    .Select(vocab => vocab.Id).ToListAsync(cancellationToken);
            var available = availableVocabIds.ToHashSet();
            questions = attempt.Questions.OrderBy(question => question.Position).Select(question =>
            {
                var selected = selections[question.Position];
                Guid? reviewVocabId = selected != question.CorrectChoiceIndex &&
                    question.SourceVocabId is Guid id && available.Contains(id) ? id : null;
                return new MockTestReviewQuestionDto(question.Word, question.Reading, question.Choices,
                    selected, question.CorrectChoiceIndex, reviewVocabId);
            }).ToList();
        }

        return new MockTestReviewDto(result.Id, result.Level, result.TotalQuestions, result.CorrectAnswers,
            result.ScorePercentage, result.Duration, result.TakenAt, questions);
    }
}
