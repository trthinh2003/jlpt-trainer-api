using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
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
        // lấy danh sách thẻ tới hạn ôn cho phiên học hiện tại
        [HttpGet("due")]
        public async Task<ActionResult<List<DueCardDto>>> GetDueCards([FromQuery] int maxCards = 20, CancellationToken cancellationToken = default)
        {
            var result = await mediator.Send(new GetDueCardsQuery(maxCards), cancellationToken);
            return Ok(result);
        }

        // thêm 1 Vocab/Kanji/Grammar vào bộ ôn tập của user hiện tại
        [HttpPost]
        public async Task<ActionResult<Guid>> AddToReview(AddToReviewCommand command, CancellationToken cancellationToken)
        {
            var id = await mediator.Send(command, cancellationToken);
            return Ok(id);
        }

        // gửi kết quả sau khi user lật thẻ và chọn Again/Hard/Good/Easy
        [HttpPost("{id:guid}/review")]
        public async Task<ActionResult<SubmitReviewResult>> SubmitReview(Guid id, [FromBody] SubmitReviewRequest request, CancellationToken cancellationToken = default)
        {
            var result = await mediator.Send(new SubmitReviewCommand(id, request.Grade), cancellationToken);

            return Ok(result);
        }
    }

    public sealed record SubmitReviewRequest(ReviewGrade Grade);
}
