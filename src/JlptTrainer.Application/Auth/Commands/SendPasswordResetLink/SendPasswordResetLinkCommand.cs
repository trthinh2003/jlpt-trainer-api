using MediatR;

namespace JlptTrainer.Application.Auth.Commands.SendPasswordResetLink;

public sealed record SendPasswordResetLinkCommand(string Email) : IRequest;
