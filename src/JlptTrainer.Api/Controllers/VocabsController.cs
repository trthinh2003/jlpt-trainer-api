using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Application.Vocabs.Commands.DeleteVocab;
using JlptTrainer.Application.Vocabs.ImportVocabFromExcel;
using JlptTrainer.Application.Vocabs.Queries.GetVocabById;
using JlptTrainer.Application.Vocabs.Queries.GetVocabImportTemplate;
using JlptTrainer.Application.Vocabs.Queries.GetVocabList;
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

        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            CreateVocabCommand command,
            CancellationToken cancellationToken)
        {
            var id = await mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

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

        // import hàng loạt Vocab từ file Excelormat (dòng đầu là header)
        [HttpPost("import")]
        [RequestSizeLimit(5 * 1024 * 1024)] // khớp với giới hạn 5MB đã validate ở Command
        public async Task<ActionResult<ImportVocabResult>> Import(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                return BadRequest("File không được để trống.");
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);

            var result = await mediator.Send(
                new ImportVocabFromExcelCommand(memoryStream.ToArray()),
                cancellationToken);

            return Ok(result);
        }
    }
}
