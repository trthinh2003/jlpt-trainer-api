using FluentValidation;
using FluentValidation.Results;
using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IRequestHandler<ResetPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var hash = PasswordResetToken.Hash(request.Token);
        var now = timeProvider.GetUtcNow();
        var user = await dbContext.Users.SingleOrDefaultAsync(
            u => u.PasswordResetTokenHash == hash && u.PasswordResetExpiresAt > now, cancellationToken)
            ?? throw InvalidResetLink();

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw InvalidResetLink();
        }

        return Unit.Value;
    }

    private static ValidationException InvalidResetLink() => new([
        new ValidationFailure(nameof(ResetPasswordCommand.Token), "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.")
    ]);
}
