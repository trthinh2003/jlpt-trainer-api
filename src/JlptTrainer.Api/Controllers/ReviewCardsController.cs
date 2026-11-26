using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
using JlptTrainer.Application.ReviewCards;
using JlptTrainer.Application.ReviewCards.Commands.AddReviewCardsBatch;
using JlptTrainer.Application.ReviewCards.Commands.RemoveStudyCard;
using JlptTrainer.Application.ReviewCards.Commands.SubmitReview;
using JlptTrainer.Application.ReviewCards.Queries.GetDueCards;
using JlptTrainer.Application.ReviewCards.Queries.ListStudyCards;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace JlptTrainer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewCardsController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<StudyCardPage>> List([FromQuery] ListStudyCards query, CancellationToken ct) =>
            Ok(await mediator.Send(query, ct));

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
        {
            await mediator.Send(new RemoveStudyCard(id), ct);
            return NoContent();
        }

        // lấy danh sách thẻ tới hạn ôn cho phiên học hiện tại
        [HttpGet("due")]
        public async Task<ActionResult<List<DueCardDto>>> GetDueCards(
            [FromQuery] int maxCards = 20,
            [FromQuery] CardType? cardType = null,
            [FromQuery] JlptLevel? level = null,
            CancellationToken cancellationToken = default)
        {
            var result = await mediator.Send(new GetDueCardsQuery(maxCards, cardType, level), cancellationToken);
            return Ok(result);
        }

        // thêm 1 Vocab/Kanji/Grammar vào bộ ôn tập của user hiện tại
        [HttpPost]
        public async Task<ActionResult<Guid>> AddToReview(AddToReviewCommand command, CancellationToken cancellationToken)
        {
            var id = await mediator.Send(command, cancellationToken);
            return Ok(id);
        }

        [HttpPost("batch")]
        public async Task<ActionResult<ReviewCardAddition>> AddBatch(
            AddReviewCardsBatchRequest request, CancellationToken cancellationToken)
        {
            return Ok(await mediator.Send(new AddReviewCardsBatchCommand(request.CardType, request.ReferenceIds), cancellationToken));
        }

        [HttpPost("kanji/batch")]
        public Task<ActionResult<ReviewCardAddition>> AddKanjiBatch(
            AddKanjiBatchRequest request, CancellationToken cancellationToken) =>
            AddBatch(new AddReviewCardsBatchRequest(CardType.Kanji, request.ReferenceIds), cancellationToken);

        // gửi kết quả sau khi user lật thẻ và chọn Again/Hard/Good/Easy
        [HttpPost("{id:guid}/review")]
        public async Task<ActionResult<SubmitReviewResult>> SubmitReview(Guid id, [FromBody] SubmitReviewRequest request, CancellationToken cancellationToken = default)
        {
            var result = await mediator.Send(new SubmitReviewCommand(id, request.Grade, request.RequestId), cancellationToken);

            return Ok(result);
        }
    }

    public sealed record AddReviewCardsBatchRequest([property: JsonRequired] CardType CardType, Guid[] ReferenceIds);
    public sealed record AddKanjiBatchRequest(Guid[] ReferenceIds);
    public sealed record SubmitReviewRequest(ReviewGrade Grade, Guid? RequestId = null);
}
