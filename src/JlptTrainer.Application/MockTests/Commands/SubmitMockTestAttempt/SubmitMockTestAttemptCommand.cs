using FluentValidation;
using FluentValidation.Results;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.MockTests.Commands.SubmitMockTestAttempt;

public sealed record SubmitMockTestAttemptCommand(Guid AttemptId, int?[] SelectedChoices) : IRequest<Guid>;

public sealed class SubmitMockTestAttemptCommandValidator : AbstractValidator<SubmitMockTestAttemptCommand>
{
    public SubmitMockTestAttemptCommandValidator()
    {
        RuleFor(request => request.SelectedChoices).NotEmpty().Must(choices => choices is { Length: <= 50 });
        RuleForEach(request => request.SelectedChoices).Must(choice => choice is null or >= 0 and <= 3)
            .WithMessage("Phương án đã chọn phải có chỉ số từ 0 đến 3.");
    }
}

public sealed class SubmitMockTestAttemptCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IRequestHandler<SubmitMockTestAttemptCommand, Guid>
{
    public async Task<Guid> Handle(SubmitMockTestAttemptCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var attempt = await dbContext.MockTestAttempts.AsNoTracking()
            .Include(entry => entry.Questions)
            .SingleOrDefaultAsync(entry => entry.Id == request.AttemptId && entry.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(MockTestAttempt), request.AttemptId);
        var submissions = dbContext.MockTests.AsNoTracking().Where(test => test.AttemptId == attempt.Id && test.UserId == userId);
        var submitted = await submissions.SingleOrDefaultAsync(cancellationToken);
        if (submitted is not null) return GetMatchingResult(submitted, request.SelectedChoices);

        var submittedAt = timeProvider.GetUtcNow();
        if (submittedAt >= attempt.ExpiresAt) throw new MockTestAttemptExpiredException();
        if (request.SelectedChoices.Length != attempt.Questions.Count)
        {
            throw new ValidationException([
                new ValidationFailure(nameof(request.SelectedChoices), "Số câu trả lời phải khớp với đề đã cấp.")
            ]);
        }

        var result = new MockTest
        {
            AttemptId = attempt.Id,
            SelectedChoices = request.SelectedChoices,
            UserId = userId,
            Level = attempt.Level,
            TotalQuestions = attempt.Questions.Count,
            CorrectAnswers = attempt.Questions.Count(question =>
                request.SelectedChoices[question.Position] == question.CorrectChoiceIndex),
            Duration = submittedAt - attempt.CreatedAt,
            CreatedAt = submittedAt,
            TakenAt = submittedAt,
        };
        dbContext.MockTests.Add(result);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return result.Id;
        }
        catch (DbUpdateException)
        {
            submitted = await submissions.SingleOrDefaultAsync(cancellationToken);
            if (submitted is null) throw;
            return GetMatchingResult(submitted, request.SelectedChoices);
        }
    }

    private static Guid GetMatchingResult(MockTest submitted, int?[] selections)
    {
        if (submitted.SelectedChoices is not null && submitted.SelectedChoices.SequenceEqual(selections)) return submitted.Id;
        throw new ConflictException("Phiên đã nộp với câu trả lời khác. Hãy tải lại bài để xem kết quả.");
    }
}
