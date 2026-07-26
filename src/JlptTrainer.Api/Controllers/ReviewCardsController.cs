using JlptTrainer.Application.ReviewCards.Commands.SubmitReview;
using JlptTrainer.Application.ReviewCards.Queries.GetDueCards;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JlptTrainer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewCardsController(ISender mediator) : ControllerBase
    {
        // lấy danh sách thẻ tới hạn ôn cho phiên học hiện tạ
        [HttpGet("due")]
        public async Task<ActionResult<List<DueCardDto>>> GetDueCards(
            [FromQuery] int maxCards = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await mediator.Send(new GetDueCardsQuery(maxCards), cancellationToken);
            return Ok(result);
        }

        // gửi kết quả sau khi user lật thẻ và chọn Again/Hard/Good/Easy
        [HttpPost("{id:guid}/review")]
        public async Task<ActionResult<SubmitReviewResult>> SubmitReview(
            Guid id,
            [FromBody] SubmitReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await mediator.Send(
                new SubmitReviewCommand(id, request.Grade),
                cancellationToken);

            return Ok(result);
        }
    }

    public sealed record SubmitReviewRequest(ReviewGrade Grade);
}
