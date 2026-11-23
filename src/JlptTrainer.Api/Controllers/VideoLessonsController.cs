using JlptTrainer.Api.Auth;
using JlptTrainer.Application.VideoLessons;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JlptTrainer.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/video-lessons")]
public sealed class VideoLessonsController(ISender mediator) : ControllerBase
{
    private const long MaxRequestSize = PublishVideoLesson.MaxSize + 32 * 1024;

    [HttpGet]
    public async Task<ActionResult<VideoLessonPage>> List([FromQuery] ListVideoLessons query, CancellationToken ct) =>
        Ok(await mediator.Send(query, ct));

    [HttpGet("{id:guid}/playback")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<VideoLessonPlayback>> Playback(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetVideoLessonPlayback(id), ct));

    [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestSize)]
    public async Task<ActionResult<Guid>> Publish([FromForm] string title, [FromForm] string? description,
        [FromForm] JlptLevel level, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var id = await mediator.Send(new PublishVideoLesson(title, description, level, stream, file.Length), ct);
        return CreatedAtAction(nameof(Playback), new { id }, id);
    }

    [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
    {
        await mediator.Send(new UnpublishVideoLesson(id), ct);
        return NoContent();
    }
}
