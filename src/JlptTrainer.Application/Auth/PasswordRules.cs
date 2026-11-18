using System.Text;
using FluentValidation;

namespace JlptTrainer.Application.Auth;

internal static class PasswordRules
{
    public static void ApplyTo<T>(IRuleBuilder<T, string> rule)
    {
        rule.NotEmpty()
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
            .Matches("[A-Z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ hoa.")
            .Matches("[0-9]").WithMessage("Mật khẩu phải có ít nhất 1 chữ số.")
            .Must(password => password is null || Encoding.UTF8.GetByteCount(password) <= 72)
            .WithMessage("Mật khẩu tối đa 72 byte UTF-8.");
    }
}
