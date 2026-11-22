using JlptTrainer.Application.GrammarPoints.Commands.CreateGrammarPoint;
using JlptTrainer.Application.GrammarPoints.Commands.DeleteGrammarPoint;
using JlptTrainer.Application.GrammarPoints.Commands.ImportGrammarPointFromExcel;
using JlptTrainer.Application.GrammarPoints.Commands.UpdateGrammarPointContent;
using JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointById;
using JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointImportTemplate;
using JlptTrainer.Application.GrammarPoints.Queries.GetGrammarPointList;
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

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            CreateGrammarPointCommand command,
            CancellationToken cancellationToken)
        {
            var id = await mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPut("{id:guid}/content")]
        public async Task<IActionResult> UpdateContent(Guid id, UpdateGrammarPointContentRequest request,
            CancellationToken cancellationToken)
        {
            await mediator.Send(new UpdateGrammarPointContentCommand(id, request.Meaning,
                request.ExampleSentence, request.ExampleSentenceMeaning), cancellationToken);
            return NoContent();
        }

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
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

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPost("import")]
        [RequestSizeLimit(CatalogExcelUpload.RequestSizeLimit)]
        public Task<ActionResult<CatalogImportResponse>> Import(IFormFile file, CancellationToken cancellationToken) =>
            CatalogExcelUpload.HandleAsync(file, mediator, content => new ImportGrammarPointFromExcelCommand(content),
                CatalogImportResponse.From, cancellationToken);

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPost("import/preview")]
        [RequestSizeLimit(CatalogExcelUpload.RequestSizeLimit)]
        public Task<ActionResult<CatalogImportPreviewResponse>> PreviewImport(IFormFile file, CancellationToken cancellationToken) =>
            CatalogExcelUpload.HandleAsync(file, mediator, content => new ImportGrammarPointFromExcelCommand(content, PreviewOnly: true),
                CatalogImportPreviewResponse.From, cancellationToken);

        public sealed record UpdateGrammarPointContentRequest(string Meaning,
            string? ExampleSentence, string? ExampleSentenceMeaning);
    }
}
