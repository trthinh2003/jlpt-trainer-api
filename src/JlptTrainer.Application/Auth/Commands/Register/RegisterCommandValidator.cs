using FluentValidation;

namespace JlptTrainer.Application.Auth.Commands.Register
{
    public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress().WithMessage("Email không đúng định dạng.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
                .Matches("[A-Z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ hoa.")
                .Matches("[0-9]").WithMessage("Mật khẩu phải có ít nhất 1 chữ số.");

            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
