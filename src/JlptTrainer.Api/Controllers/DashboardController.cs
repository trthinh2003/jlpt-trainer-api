using JlptTrainer.Application.Dashboard.Queries.GetProgressStats;
using JlptTrainer.Application.Dashboard.Queries.GetStudyHeatmap;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JlptTrainer.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController(ISender mediator) : ControllerBase
    {
        [HttpGet("heatmap")] // heatmap
        public async Task<ActionResult<List<HeatmapEntryDto>>> GetHeatmap(
            [FromQuery] int days = 365,
            CancellationToken cancellationToken = default)
        {
            var result = await mediator.Send(new GetStudyHeatmapQuery(days), cancellationToken);
            return Ok(result);
        }

        // tiến độ theo loại thẻ (Vocab/Kanji/Grammar) + xu hướng điểm Mock Test
        [HttpGet("progress")]
        public async Task<ActionResult<ProgressStatsResult>> GetProgress(CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetProgressStatsQuery(), cancellationToken);
            return Ok(result);
        }
    }
}
