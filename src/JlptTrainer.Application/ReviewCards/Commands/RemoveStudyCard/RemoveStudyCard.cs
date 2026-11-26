using FluentValidation;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.ReviewCards.Commands.RemoveStudyCard;

public sealed record RemoveStudyCard(Guid Id) : IRequest;

public sealed class RemoveStudyCardValidator : AbstractValidator<RemoveStudyCard>
{
    public RemoveStudyCardValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class RemoveStudyCardHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<RemoveStudyCard>
{
    public async Task Handle(RemoveStudyCard request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var removed = await db.ReviewCards.Where(card => card.Id == request.Id && card.UserId == userId)
            .ExecuteDeleteAsync(ct);
        if (removed == 0) throw new NotFoundException(nameof(ReviewCard), request.Id);
    }
}
