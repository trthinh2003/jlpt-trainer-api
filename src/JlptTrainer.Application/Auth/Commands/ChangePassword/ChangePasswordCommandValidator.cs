using FluentValidation;

namespace JlptTrainer.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại.");

        PasswordRules.ApplyTo(RuleFor(command => command.NewPassword));
        RuleFor(command => command.NewPassword)
            .NotEqual(command => command.CurrentPassword)
            .WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại.");
    }
}
