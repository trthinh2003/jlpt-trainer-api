using JlptTrainer.Api.Auth;
using JlptTrainer.Application.AudioLessons;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JlptTrainer.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/audio-lessons")]
public sealed class AudioLessonsController(ISender mediator) : ControllerBase
{
    private const long MaxRequest = PublishAudioLesson.MaxAudioBytes + PublishAudioLesson.MaxCoverBytes + 160 * 1024;

    [HttpGet]
    public async Task<ActionResult<AudioLessonPage>> List([FromQuery] ListAudioLessons query, CancellationToken ct) =>
        Ok(await mediator.Send(query, ct));

    [HttpGet("{id:guid}/playback")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<AudioLessonPlayback>> Playback(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetAudioLesson(id), ct));

    [HttpGet("{id:guid}/words")]
    public async Task<ActionResult<IReadOnlyList<AudioLessonVocab>>> Words(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetAudioLessonWords(id), ct));

    [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
    [HttpPost("preview-lyrics")]
    [RequestSizeLimit(192 * 1024)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<LyricLine[]>> PreviewLyrics([FromBody] PreviewAudioLyrics request, CancellationToken ct) =>
        Ok(await mediator.Send(request, ct));

    [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequest)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequest)]
    public async Task<ActionResult<Guid>> Publish([FromForm] PublishForm form, CancellationToken ct)
    {
        var id = await mediator.Send(new PublishAudioLesson(form.Title, form.Level, form.Lyrics, form.VocabIds,
            form.Audio.OpenReadStream, form.Audio.Length, form.Cover.OpenReadStream, form.Cover.Length), ct);
        return CreatedAtAction(nameof(Playback), new { id }, id);
    }

    [Authorize(Policy = CatalogAuthorization.ManagePolicy)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
    {
        await mediator.Send(new UnpublishAudioLesson(id), ct);
        return NoContent();
    }

    public sealed class PublishForm
    {
        public string Title { get; init; } = "";
        public JlptLevel Level { get; init; }
        public string Lyrics { get; init; } = "";
        public Guid[] VocabIds { get; init; } = [];
        public IFormFile Audio { get; init; } = null!;
        public IFormFile Cover { get; init; } = null!;
    }
}
