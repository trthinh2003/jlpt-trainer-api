using MediatR;

namespace JlptTrainer.Application.Auth.Commands.Register
{
    public sealed record RegisterCommand(
        string Email,
        string Password,
        string DisplayName) : IRequest<AuthResult>;
}
