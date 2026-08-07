using Dapper;
using JlptTrainer.Application.Common.Interfaces;
using MediatR;

namespace JlptTrainer.Application.Dashboard.Queries.GetStudyHeatmap
{
    public sealed class GetStudyHeatmapQueryHandler(
        IDapperContext dapperContext,
        ICurrentUserService currentUser,
        TimeProvider timeProvider
    ) : IRequestHandler<GetStudyHeatmapQuery, List<HeatmapEntryDto>>
    {
        private const string Sql = """
            SELECT study_date AS studydate, review_count AS reviewcount
            FROM get_study_heatmap(@UserId, @FromDate);
        """;

        public async Task<List<HeatmapEntryDto>> Handle(GetStudyHeatmapQuery request, CancellationToken cancellationToken)
        {
            using var connection = dapperContext.CreateConnection();

            var fromDate = timeProvider.GetUtcNow().AddDays(-request.Days);

            var result = await connection.QueryAsync<HeatmapEntryDto>(
                new CommandDefinition(
                    Sql,
                    new { UserId = currentUser.UserId, FromDate = fromDate },
                    cancellationToken: cancellationToken));

            return result.ToList();
        }
    }
}
