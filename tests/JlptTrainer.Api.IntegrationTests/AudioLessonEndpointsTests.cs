using System.Net;
using System.Net.Http.Json;
using JlptTrainer.Application.AudioLessons;
using JlptTrainer.Application.AudioLessons.Lyrics;
using JlptTrainer.Application.AudioLessons.Queries.GetAudioLessonPlayback;
using JlptTrainer.Application.AudioLessons.Queries.ListAudioLessons;
using JlptTrainer.Application.AudioLessons.Queries.PreviewAudioLyrics;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class AudioLessonEndpointsTests(AudioLessonWebApplicationFactory factory)
    : IClassFixture<AudioLessonWebApplicationFactory>
{
    private const string Path = "/api/audio-lessons";

    [Fact]
    public async Task ManagerPublishesTimedLyricsAndWordsRemainLinkedToTheCatalog()
    {
        using var manager = factory.CreateClient();
        using var learner = factory.CreateClient();
        using var guest = factory.CreateClient();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        await learner.AuthenticateAsync();
        Guid morning = await CreateWordAsync(manager, "朝", "あさ", "buổi sáng");
        Guid oboro = await CreateWordAsync(manager, "朧", "おぼろ", "mờ ảo");

        var lyrics = $"[ar:Original]\n[00:02.0]🌙[[朧|{oboro:N}]]な空。\n[00:00.00][00:03.15]おはよう。\n[00:00.00]Ohayō.\n[offset:150]";
        const string previewPath = Path + "/preview-lyrics";
        var previewRequest = new PreviewAudioLyrics(lyrics, [oboro, morning]);
        using var guestPreview = await guest.PostAsJsonAsync(previewPath, previewRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, guestPreview.StatusCode);
        using var learnerPreview = await learner.PostAsJsonAsync(previewPath, previewRequest);
        Assert.Equal(HttpStatusCode.Forbidden, learnerPreview.StatusCode);
        using var invalidPreview = await manager.PostAsJsonAsync(previewPath, new PreviewAudioLyrics("[00:70.00]無効"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidPreview.StatusCode);
        using var unselectedPreview = await manager.PostAsJsonAsync(previewPath, new PreviewAudioLyrics(lyrics, [morning]));
        Assert.Equal(HttpStatusCode.BadRequest, unselectedPreview.StatusCode);
        using var supportLinkPreview = await manager.PostAsJsonAsync(previewPath,
            new PreviewAudioLyrics($"[00:00.0]朧。\n[00:00.0][[Oboro|{oboro:N}]]", [oboro]));
        Assert.Equal(HttpStatusCode.BadRequest, supportLinkPreview.StatusCode);
        using var preview = await manager.PostAsJsonAsync(previewPath, previewRequest);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.True(preview.Headers.CacheControl?.NoStore);
        var previewLyrics = (await preview.Content.ReadFromJsonAsync<LyricLine[]>())!;
        Assert.Equal(new[] { (150, "おはよう。\nOhayō."), (2150, "🌙朧な空。"), (3300, "おはよう。") },
            previewLyrics.Select(line => (line.StartMs, line.Text)));
        Assert.Null(previewLyrics[0].Links);
        var link = new LyricWordLink(2, 1, new AudioLessonVocab(oboro, "朧", "おぼろ", "mờ ảo"));
        Assert.Equal(link, Assert.Single(previewLyrics[1].Links!));
        Assert.Empty((await manager.GetFromJsonAsync<AudioLessonPage>(Path))!.Items);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync(Path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PublishAsync(learner, lyrics, [oboro, morning])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PublishAsync(manager, "[00:70.00]無効", [oboro])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PublishAsync(manager, lyrics, [morning])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PublishAsync(manager, lyrics, [morning, morning])).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await PublishAsync(manager, lyrics, [Guid.NewGuid()])).StatusCode);
        Assert.Empty(factory.Storage.Uploads);

        using var created = await PublishAsync(manager, lyrics, [oboro, morning]);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = await created.Content.ReadFromJsonAsync<Guid>();
        var root = $"{Path}/{id}";
        Assert.Equal(new AudioLessonItem(id, "Một buổi sáng", JlptLevel.N4),
            Assert.Single((await learner.GetFromJsonAsync<AudioLessonPage>(Path + "?level=4"))!.Items));

        using var playback = await learner.GetAsync(root + "/playback");
        Assert.True(playback.Headers.CacheControl?.NoStore);
        var media = (await playback.Content.ReadFromJsonAsync<AudioLessonPlayback>())!;
        Assert.Equal(previewLyrics.Select(line => (line.StartMs, line.Text)), media.Lyrics.Select(line => (line.StartMs, line.Text)));
        Assert.Equal(link, Assert.Single(media.Lyrics[1].Links!));
        Assert.EndsWith("/audio.wav", media.AudioUrl);
        Assert.EndsWith("/cover.webp", media.CoverUrl);
        Assert.Equal(new[] { "audio/wav", "image/webp" }, factory.Storage.Uploads);
        var words = await learner.GetFromJsonAsync<AudioLessonVocab[]>(root + "/words");
        Assert.Equal(new[] { oboro, morning }, words!.Select(word => word.Id));

        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync($"/api/vocabs/{oboro}")).StatusCode);
        Assert.Equal(morning, Assert.Single((await learner.GetFromJsonAsync<AudioLessonVocab[]>(root + "/words"))!).Id);
        var updatedPlayback = (await learner.GetFromJsonAsync<AudioLessonPlayback>(root + "/playback"))!;
        Assert.Equal("🌙朧な空。", updatedPlayback.Lyrics[1].Text);
        Assert.Null(updatedPlayback.Lyrics[1].Links);
        Assert.Equal(HttpStatusCode.Forbidden, (await learner.DeleteAsync(root)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync(root)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync(root + "/playback")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync(root + "/words")).StatusCode);
    }

    internal static async Task<Guid> CreateWordAsync(HttpClient manager, string word, string reading, string meaning)
    {
        using var response = await manager.PostAsJsonAsync("/api/vocabs",
            new CreateVocabCommand(word, reading, meaning, null, null, JlptLevel.N4));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    internal static async Task<HttpResponseMessage> PublishAsync(HttpClient client, string lyrics, Guid[] words)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(" Một buổi sáng "), "title");
        form.Add(new StringContent("4"), "level");
        form.Add(new StringContent(lyrics), "lyrics");
        foreach (var word in words) form.Add(new StringContent(word.ToString()), "vocabIds");
        form.Add(new ByteArrayContent(WaveFile()), "audio", "sample.wav");
        using var bitmap = new SKBitmap(4, 4);
        bitmap.Erase(SKColors.SkyBlue);
        using var cover = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        form.Add(new ByteArrayContent(cover.ToArray()), "cover", "sample.png");
        return await client.PostAsync(Path, form);
    }

    private static byte[] WaveFile()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(40);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(8000);
        writer.Write(16000);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(4);
        writer.Write(0);
        return stream.ToArray();
    }
}

public sealed class AudioLessonWebApplicationFactory : CustomWebApplicationFactory
{
    public AudioStorage Storage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSingleton<IMediaObjectStorage>(Storage));
    }

    public sealed class AudioStorage : IMediaObjectStorage
    {
        public List<string> Uploads { get; } = [];
        public bool IsConfigured => true;
        public Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct)
        {
            Assert.Equal(0, stream.Position);
            Uploads.Add(contentType);
            return Task.CompletedTask;
        }
        public Task<string> SignReadAsync(string key, DateTimeOffset expiresAt, CancellationToken ct) =>
            Task.FromResult($"https://media.example.invalid/{key}");
        public Task DeleteAsync(string key, CancellationToken ct) => Task.CompletedTask;
    }
}
