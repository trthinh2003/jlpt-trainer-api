using JlptTrainer.Application.Auth.Commands.Register;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Auth.Commands.Login
{
    public sealed class LoginCommandHandler(
       IApplicationDbContext dbContext,
       IPasswordHasher passwordHasher,
       ITokenGenerator tokenGenerator)
       : IRequestHandler<LoginCommand, AuthResult>
    {
        public async Task<AuthResult> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new InvalidCredentialsException();
            }

            var token = tokenGenerator.GenerateToken(user);

            return new AuthResult(user.Id, user.Email, user.DisplayName, token);
        }
    }
}
