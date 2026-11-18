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

            PasswordRules.ApplyTo(RuleFor(x => x.Password));

            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
