using MediatR;

namespace JlptTrainer.Application.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Unit>;
