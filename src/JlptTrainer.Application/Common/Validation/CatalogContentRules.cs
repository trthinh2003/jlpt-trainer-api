using FluentValidation;

namespace JlptTrainer.Application.Common.Validation;

internal static class CatalogContentRules
{
    public static IRuleBuilderOptions<T, string> ValidMeaning<T>(this IRuleBuilder<T, string> rule)
        => rule.NotEmpty().MaximumLength(500);
}
