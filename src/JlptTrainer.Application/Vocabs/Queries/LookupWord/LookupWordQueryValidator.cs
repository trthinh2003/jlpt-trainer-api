using FluentValidation;

namespace JlptTrainer.Application.Vocabs.Queries.LookupWord
{
    public sealed class LookupWordQueryValidator : AbstractValidator<LookupWordQuery>
    {
        public LookupWordQueryValidator()
        {
            RuleFor(x => x.Keyword)
                .NotEmpty()
                .MaximumLength(50);
        }
    }
}
