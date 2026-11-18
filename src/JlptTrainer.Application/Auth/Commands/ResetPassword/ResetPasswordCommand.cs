using MediatR;

namespace JlptTrainer.Application.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Unit>;
