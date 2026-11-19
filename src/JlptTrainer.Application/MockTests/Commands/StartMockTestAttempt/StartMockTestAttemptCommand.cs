using FluentValidation;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.MockTests.Commands.StartMockTestAttempt;

public sealed record StartMockTestAttemptCommand(JlptLevel Level, int QuestionCount = 10) : IRequest<MockTestAttemptDto>;

public sealed class StartMockTestAttemptCommandValidator : AbstractValidator<StartMockTestAttemptCommand>
{
    public StartMockTestAttemptCommandValidator()
    {
        RuleFor(request => request.Level).IsInEnum();
        RuleFor(request => request.QuestionCount).InclusiveBetween(1, 50);
    }
}

public sealed class StartMockTestAttemptCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IRequestHandler<StartMockTestAttemptCommand, MockTestAttemptDto>
{
    private static readonly TimeSpan AttemptDuration = TimeSpan.FromMinutes(15);

    public async Task<MockTestAttemptDto> Handle(StartMockTestAttemptCommand request, CancellationToken cancellationToken)
    {
        var questions = await MockTestQuestionGenerator.GenerateAsync(dbContext, request.Level, request.QuestionCount, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var startedAt = now.AddTicks(-(now.Ticks % 10));
        var attempt = new MockTestAttempt(currentUser.UserId, request.Level, startedAt, startedAt + AttemptDuration);
        attempt.Questions.AddRange(questions.Select((question, position) => new MockTestAttemptQuestion(
            position, question.VocabId, question.Word, question.Reading, question.Choices, question.CorrectChoiceIndex)));
        dbContext.MockTestAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MockTestAttemptDto.Create(attempt, timeProvider.GetUtcNow());
    }
}
