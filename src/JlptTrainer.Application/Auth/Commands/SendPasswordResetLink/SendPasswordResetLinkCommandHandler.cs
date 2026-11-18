using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Auth.Commands.SendPasswordResetLink;

public sealed class SendPasswordResetLinkCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordResetEmailSender emailSender,
    TimeProvider timeProvider) : IRequestHandler<SendPasswordResetLinkCommand>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ResendDelay = TimeSpan.FromMinutes(1);

    public async Task Handle(SendPasswordResetLinkCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (user is null || user.PasswordResetExpiresAt > now + TokenLifetime - ResendDelay) return;

        var token = PasswordResetToken.Create();
        user.SetPasswordResetToken(PasswordResetToken.Hash(token), now + TokenLifetime);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return; // A concurrent reset or password change superseded this request.
        }

        await emailSender.SendAsync(user.Email, token, cancellationToken);
    }
}
