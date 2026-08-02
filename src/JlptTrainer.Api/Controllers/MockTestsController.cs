using JlptTrainer.Application.MockTests.Commands.SubmitMockTest;
using JlptTrainer.Application.MockTests.Queries.GetMockTestHistory;
using JlptTrainer.Application.MockTests.Queries.GetMockTestQuestions;
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

        // nộp bài, chấm điểm và lưu kết quả
        [HttpPost("submit")]
        public async Task<ActionResult<SubmitMockTestResult>> Submit(SubmitMockTestCommand command, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        // lịch sử làm bài của user hiện tại
        [HttpGet("history")]
        public async Task<ActionResult<PagedMockTestHistoryResult>> GetHistory([FromQuery] GetMockTestHistoryQuery query, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
