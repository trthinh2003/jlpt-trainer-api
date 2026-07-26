using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.Auth.Commands.Register
{
    public sealed class RegisterCommandHandler(
       IApplicationDbContext dbContext,
       IPasswordHasher passwordHasher,
       ITokenGenerator tokenGenerator)
       : IRequestHandler<RegisterCommand, AuthResult>
    {
        public async Task<AuthResult> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            // email so sánh không phân biệt hoa/thường - "A@b.com" và "a@b.com" là cùng 1 user
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var emailExists = await dbContext.Users
                .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (emailExists)
            {
                throw new ConflictException($"Email \"{request.Email}\" đã được đăng ký.");
            }

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = passwordHasher.Hash(request.Password),
                DisplayName = request.DisplayName.Trim()
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            var token = tokenGenerator.GenerateToken(user);

            return new AuthResult(user.Id, user.Email, user.DisplayName, token);
        }
    }
}
