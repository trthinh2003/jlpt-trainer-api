using FluentValidation;
using MediatR;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestHistory
{
    public sealed record GetMockTestHistoryQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedMockTestHistoryResult>;

    public sealed class GetMockTestHistoryQueryValidator : AbstractValidator<GetMockTestHistoryQuery>
    {
        public GetMockTestHistoryQueryValidator()
        {
            RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
            RuleFor(query => query.PageNumber).GreaterThan(0);
            RuleFor(query => query).Must(query => ((long)query.PageNumber - 1) * query.PageSize <= int.MaxValue)
                .WithMessage("Vị trí trang vượt quá phạm vi hỗ trợ.");
        }
    }
}
