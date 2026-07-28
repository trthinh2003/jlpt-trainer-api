using JlptTrainer.Application.Kanjis.Commands.CreateKanji;
using JlptTrainer.Application.Kanjis.Commands.DeleteKanji;
using JlptTrainer.Application.Kanjis.Queries.GetKanjiById;
using JlptTrainer.Application.Kanjis.Queries.GetKanjiList;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JlptTrainer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KanjisController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PagedKanjiResult>> GetList([FromQuery] GetKanjiListQuery query, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<KanjiDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetKanjiByIdQuery(id), cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create(CreateKanjiCommand command, CancellationToken cancellationToken)
        {
            var id = await mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await mediator.Send(new DeleteKanjiCommand(id), cancellationToken);
            return NoContent();
        }
    }
}
