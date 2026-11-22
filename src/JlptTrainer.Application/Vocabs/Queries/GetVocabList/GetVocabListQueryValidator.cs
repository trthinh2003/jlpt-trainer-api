using FluentValidation;

namespace JlptTrainer.Application.Vocabs.Queries.GetVocabList
{
    public sealed class GetVocabListQueryValidator : AbstractValidator<GetVocabListQuery>
    {
        public GetVocabListQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100)
                .WithMessage("PageSize phải trong khoảng 1-100.");
            RuleFor(x => x.Search).MaximumLength(100)
                .WithMessage("Từ khóa tìm kiếm tối đa 100 ký tự.");
        }
    }
}
