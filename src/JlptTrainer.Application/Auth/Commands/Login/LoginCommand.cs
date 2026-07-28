using JlptTrainer.Application.Auth.Commands.Register;
using MediatR;

namespace JlptTrainer.Application.Auth.Commands.Login
{
    public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResult>;
}
