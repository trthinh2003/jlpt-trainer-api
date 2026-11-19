using JlptTrainer.Application.MockTests;
using JlptTrainer.Application.MockTests.Commands.StartMockTestAttempt;
using JlptTrainer.Application.MockTests.Commands.SubmitMockTestAttempt;
using JlptTrainer.Application.MockTests.Queries.GetMockTestAttempt;
using JlptTrainer.Application.MockTests.Queries.GetMockTestHistory;
using JlptTrainer.Application.MockTests.Queries.GetMockTestQuestions;
using JlptTrainer.Application.MockTests.Queries.GetMockTestResultPdf;
using JlptTrainer.Application.MockTests.Queries.GetMockTestReview;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JlptTrainer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MockTestsController(ISender mediator) : ControllerBase
    {
        [HttpGet("questions")]
        public async Task<ActionResult<List<MockTestQuestionDto>>> GetQuestions([FromQuery] GetMockTestQuestionsQuery query, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost("submit")]
        public IActionResult SubmitLegacy() =>
            StatusCode(StatusCodes.Status410Gone, new { error = "Hãy tạo phiên làm bài mới trước khi nộp." });

        [HttpPost("attempts")]
        public async Task<ActionResult<MockTestAttemptDto>> StartAttempt(
            StartMockTestAttemptCommand command, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAttempt), new { id = result.Id }, result);
        }

        [HttpGet("attempts/{id:guid}")]
        public async Task<ActionResult<MockTestAttemptDto>> GetAttempt(Guid id, CancellationToken cancellationToken) =>
            Ok(await mediator.Send(new GetMockTestAttemptQuery(id), cancellationToken));

        [HttpPost("attempts/{id:guid}/submit")]
        public async Task<ActionResult<Guid>> SubmitAttempt(Guid id, SubmitAttemptRequest request, CancellationToken cancellationToken) =>
            Ok(await mediator.Send(new SubmitMockTestAttemptCommand(id, request.SelectedChoices), cancellationToken));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MockTestReviewDto>> GetReview(Guid id, CancellationToken cancellationToken) =>
            Ok(await mediator.Send(new GetMockTestReviewQuery(id), cancellationToken));

        public sealed record SubmitAttemptRequest(int?[] SelectedChoices);

        // lịch sử làm bài của user hiện tại
        [HttpGet("history")]
        public async Task<ActionResult<PagedMockTestHistoryResult>> GetHistory([FromQuery] GetMockTestHistoryQuery query, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // xuất PDF kết quả 1 bài thi đã làm
        [HttpGet("{id:guid}/pdf")]
        public async Task<IActionResult> GetResultPdf(Guid id, CancellationToken cancellationToken)
        {
            var bytes = await mediator.Send(new GetMockTestResultPdfQuery(id), cancellationToken);
            return File(bytes, "application/pdf", $"mocktest_result_{id}.pdf");
        }
    }
}
