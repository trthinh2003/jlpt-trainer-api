using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestAttempt;

public sealed record GetMockTestAttemptQuery(Guid AttemptId) : IRequest<MockTestAttemptDto>;

public sealed class GetMockTestAttemptQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IRequestHandler<GetMockTestAttemptQuery, MockTestAttemptDto>
{
    public async Task<MockTestAttemptDto> Handle(GetMockTestAttemptQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var attempt = await dbContext.MockTestAttempts.AsNoTracking()
            .Include(entry => entry.Questions)
            .SingleOrDefaultAsync(entry => entry.Id == request.AttemptId && entry.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(MockTestAttempt), request.AttemptId);
        var resultId = await dbContext.MockTests.Where(result => result.AttemptId == attempt.Id && result.UserId == userId)
            .Select(result => (Guid?)result.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return MockTestAttemptDto.Create(attempt, timeProvider.GetUtcNow(), resultId);
    }
}
