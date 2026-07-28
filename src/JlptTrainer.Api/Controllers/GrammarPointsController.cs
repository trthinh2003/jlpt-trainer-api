using JlptTrainer.Application.GrammarPoints.Commands.CreateGrammarPoint;
using JlptTrainer.Application.GrammarPoints.Commands.DeleteGrammarPoint;
using JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointById;
using JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointList;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JlptTrainer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GrammarPointsController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PagedGrammarPointResult>> GetList([FromQuery] GetGrammarPointListQuery query, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GrammarPointDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetGrammarPointByIdQuery(id), cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create(CreateGrammarPointCommand command, CancellationToken cancellationToken)
        {
            var id = await mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await mediator.Send(new DeleteGrammarPointCommand(id), cancellationToken);
            return NoContent();
        }
    }
}
