using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.Dashboard.Queries.GetProgressStats
{
    public sealed record CardTypeProgressDto(CardType CardType, int TotalCards, int MasteredCount);
}
