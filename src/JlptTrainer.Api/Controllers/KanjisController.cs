using JlptTrainer.Application.Kanjis.Commands.CreateKanji;
using JlptTrainer.Application.Kanjis.Commands.DeleteKanji;
using JlptTrainer.Application.Kanjis.Commands.ImportKanjiFromExcel;
using JlptTrainer.Application.Kanjis.Queries.GetKanjiById;
using JlptTrainer.Application.Kanjis.Queries.GetKanjiImportTemplate;
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

        [HttpGet("import/template")]
        public async Task<IActionResult> DownloadImportTemplate(CancellationToken cancellationToken)
        {
            var bytes = await mediator.Send(new GetKanjiImportTemplateQuery(), cancellationToken);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "kanji_import_template.xlsx");
        }

        [HttpPost("import")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<ActionResult<ImportKanjiResult>> Import(IFormFile file,CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                return BadRequest("File không được để trống.");
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);

            var result = await mediator.Send(
                new ImportKanjiFromExcelCommand(memoryStream.ToArray()),
                cancellationToken);

            return Ok(result);
        }
    }
}
