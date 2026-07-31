using JlptTrainer.Application.GrammarPoints.Commands.CreateGrammarPoint;
using JlptTrainer.Application.GrammarPoints.Commands.DeleteGrammarPoint;
using JlptTrainer.Application.GrammarPoints.Commands.ImportGrammarPointFromExcel;
using JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointById;
using JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointImportTemplate;
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
        public async Task<ActionResult<Guid>> Create(
            CreateGrammarPointCommand command,
            CancellationToken cancellationToken)
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

        [HttpGet("import/template")]
        public async Task<IActionResult> DownloadImportTemplate(CancellationToken cancellationToken)
        {
            var bytes = await mediator.Send(new GetGrammarPointImportTemplateQuery(), cancellationToken);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "grammar_point_import_template.xlsx");
        }

        [HttpPost("import")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<ActionResult<ImportGrammarPointResult>> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                return BadRequest("File không được để trống.");
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);

            var result = await mediator.Send(new ImportGrammarPointFromExcelCommand(memoryStream.ToArray()), cancellationToken);

            return Ok(result);
        }
    }
}
