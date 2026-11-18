using FluentValidation;

namespace JlptTrainer.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Token).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.")
            .Matches("\\A[0-9A-F]{64}\\z").WithMessage("Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        PasswordRules.ApplyTo(RuleFor(command => command.NewPassword));
    }
}
