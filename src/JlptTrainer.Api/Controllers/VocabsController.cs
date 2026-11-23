using JlptTrainer.Api.Auth;
using JlptTrainer.Api.ExcelImport;
using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Application.Vocabs.Commands.DeleteVocab;
using JlptTrainer.Application.Vocabs.Commands.UpdateVocabContent;
using JlptTrainer.Application.Vocabs.Images;
using JlptTrainer.Application.Vocabs.ImportVocabFromExcel;
using JlptTrainer.Application.Vocabs.Queries.GetVocabById;
using JlptTrainer.Application.Vocabs.Queries.GetVocabImportTemplate;
using JlptTrainer.Application.Vocabs.Queries.GetVocabList;
using JlptTrainer.Application.Vocabs.Queries.LookupWord;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JlptTrainer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VocabsController(ISender mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PagedVocabResult>> GetList([FromQuery] GetVocabListQuery query, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<VocabDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetVocabByIdQuery(id), cancellationToken);
            return Ok(result);
        }

        /// tra từ qua Jisho API - gợi ý cách đọc/nghĩa (tiếng Anh) trước khi tạo Vocab thật.
        [HttpGet("lookup")]
        public async Task<ActionResult<List<LookupWordResultDto>>> Lookup([FromQuery] string keyword, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new LookupWordQuery(keyword), cancellationToken);
            return Ok(result);
        }

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(CreateVocabCommand command, CancellationToken cancellationToken)
        {
            var id = await mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPut("{id:guid}/content")]
        public async Task<IActionResult> UpdateContent(Guid id, UpdateVocabContentRequest request, CancellationToken cancellationToken)
        {
            await mediator.Send(new UpdateVocabContentCommand(id, request.Meaning,
                request.ExampleSentence, request.ExampleSentenceMeaning), cancellationToken);
            return NoContent();
        }

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPut("{id:guid}/image")]
        [RequestSizeLimit(UploadVocabImage.MaxSize + 32 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(Guid id, IFormFile file, CancellationToken cancellationToken)
        {
            await using var stream = file.OpenReadStream();
            await mediator.Send(new UploadVocabImage(id, stream, file.Length), cancellationToken);
            return NoContent();
        }

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpDelete("{id:guid}/image")]
        public async Task<IActionResult> RemoveImage(Guid id, CancellationToken cancellationToken)
        {
            await mediator.Send(new RemoveVocabImage(id), cancellationToken);
            return NoContent();
        }

        [HttpGet("{id:guid}/image")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<VocabImageLink>> GetImage(Guid id, CancellationToken cancellationToken)
        {
            var link = await mediator.Send(new GetVocabImage(id), cancellationToken);
            return link is null ? NoContent() : Ok(link);
        }

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await mediator.Send(new DeleteVocabCommand(id), cancellationToken);
            return NoContent();
        }

        // tải file Excel mẫu để điền dữ liệu trước khi import
        [HttpGet("import/template")]
        public async Task<IActionResult> DownloadImportTemplate(CancellationToken cancellationToken)
        {
            var bytes = await mediator.Send(new GetVocabImportTemplateQuery(), cancellationToken);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "vocab_import_template.xlsx");
        }

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPost("import")]
        [RequestSizeLimit(CatalogExcelUpload.RequestSizeLimit)]
        public Task<ActionResult<CatalogImportResponse>> Import(IFormFile file, CancellationToken cancellationToken) =>
            CatalogExcelUpload.HandleAsync(file, mediator, content => new ImportVocabFromExcelCommand(content),
                CatalogImportResponse.From, cancellationToken);

        [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
        [HttpPost("import/preview")]
        [RequestSizeLimit(CatalogExcelUpload.RequestSizeLimit)]
        public Task<ActionResult<CatalogImportPreviewResponse>> PreviewImport(IFormFile file, CancellationToken cancellationToken) =>
            CatalogExcelUpload.HandleAsync(file, mediator, content => new ImportVocabFromExcelCommand(content, PreviewOnly: true),
                CatalogImportPreviewResponse.From, cancellationToken);

        public sealed record UpdateVocabContentRequest(string Meaning, string? ExampleSentence, string? ExampleSentenceMeaning);
    }
}
