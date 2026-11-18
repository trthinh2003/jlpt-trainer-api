using FluentValidation;
using FluentValidation.Results;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher) : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var user = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Phiên đăng nhập không còn hợp lệ.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationException([
                new ValidationFailure(nameof(request.CurrentPassword), "Mật khẩu hiện tại không đúng.")
            ]);
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Thông tin tài khoản vừa thay đổi. Vui lòng thử lại.");
        }

        return Unit.Value;
    }
}
