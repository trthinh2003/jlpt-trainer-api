using JlptTrainer.Application.Kanjis.Commands.CreateKanji;
using JlptTrainer.Application.Kanjis.Commands.DeleteKanji;
using JlptTrainer.Application.Kanjis.Commands.ImportKanjiFromExcel;
using JlptTrainer.Application.Kanjis.Commands.UpdateKanjiContent;
using JlptTrainer.Application.Kanjis.Queries.GetKanjiById;
using JlptTrainer.Application.Kanjis.Queries.GetKanjiImportTemplate;
using JlptTrainer.Application.Kanjis.Queries.GetKanjiList;
using JlptTrainer.Api.Auth;
using JlptTrainer.Api.ExcelImport;
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

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(CreateKanjiCommand command, CancellationToken cancellationToken)
        {
            var id = await mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPut("{id:guid}/content")]
        public async Task<IActionResult> UpdateContent(Guid id, UpdateKanjiContentRequest request,
            CancellationToken cancellationToken)
        {
            await mediator.Send(new UpdateKanjiContentCommand(id, request.Meaning,
                request.OnYomi, request.KunYomi), cancellationToken);
            return NoContent();
        }

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
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

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPost("import")]
        [RequestSizeLimit(CatalogExcelUpload.RequestSizeLimit)]
        public Task<ActionResult<CatalogImportResponse>> Import(IFormFile file, CancellationToken cancellationToken) =>
            CatalogExcelUpload.HandleAsync(file, mediator, content => new ImportKanjiFromExcelCommand(content),
                CatalogImportResponse.From, cancellationToken);

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPost("import/preview")]
        [RequestSizeLimit(CatalogExcelUpload.RequestSizeLimit)]
        public Task<ActionResult<CatalogImportPreviewResponse>> PreviewImport(IFormFile file, CancellationToken cancellationToken) =>
            CatalogExcelUpload.HandleAsync(file, mediator, content => new ImportKanjiFromExcelCommand(content, PreviewOnly: true),
                CatalogImportPreviewResponse.From, cancellationToken);

        public sealed record UpdateKanjiContentRequest(string Meaning,
            string? OnYomi, string? KunYomi);
    }
}
