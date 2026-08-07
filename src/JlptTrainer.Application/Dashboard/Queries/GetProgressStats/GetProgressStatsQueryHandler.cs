using Dapper;
using JlptTrainer.Application.Common.Interfaces;
using MediatR;

namespace JlptTrainer.Application.Dashboard.Queries.GetProgressStats
{
    public sealed class GetProgressStatsQueryHandler(
      IDapperContext dapperContext,
      ICurrentUserService currentUser)
      : IRequestHandler<GetProgressStatsQuery, ProgressStatsResult>
    {
        private const int MasteryThreshold = 3;

        private const string CardProgressSql = """
            SELECT card_type AS cardtype, total_cards AS totalcards, mastered_count AS masteredcount
            FROM get_card_progress(@UserId, @MasteryThreshold);
        """;

        private const string MockTestTrendSql = """
            SELECT taken_date AS takendate, level AS level,
                   average_score AS averagescore, test_count AS testcount
            FROM get_mock_test_trend(@UserId);
        """;

        public async Task<ProgressStatsResult> Handle(
            GetProgressStatsQuery request,
            CancellationToken cancellationToken)
        {
            using var connection = dapperContext.CreateConnection();
            var userId = currentUser.UserId;

            var cardProgress = await connection.QueryAsync<CardTypeProgressDto>(
                new CommandDefinition(
                    CardProgressSql,
                    new { UserId = userId, MasteryThreshold },
                    cancellationToken: cancellationToken));

            var mockTestTrend = await connection.QueryAsync<MockTestTrendDto>(
                new CommandDefinition(
                    MockTestTrendSql,
                    new { UserId = userId },
                    cancellationToken: cancellationToken));

            return new ProgressStatsResult(cardProgress.ToList(), mockTestTrend.ToList());
        }
    }
}
