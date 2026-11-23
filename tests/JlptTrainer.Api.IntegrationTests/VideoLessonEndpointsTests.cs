using System.Net;
using System.Net.Http.Json;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.VideoLessons;
using JlptTrainer.Domain.Enums;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class VideoLessonEndpointsTests(VideoLessonWebApplicationFactory factory)
    : IClassFixture<VideoLessonWebApplicationFactory>
{
    private const string Path = "/api/video-lessons";

    [Fact]
    public async Task AVideoIsVisibleOnlyAfterUploadAndCanBeUnpublishedByAManager()
    {
        using var guest = factory.CreateClient();
        using var manager = factory.CreateClient();
        using var learner = factory.CreateClient();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        await learner.AuthenticateAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync(Path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PublishAsync(learner, VideoFile())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PublishAsync(manager, [1, 2, 3])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PublishAsync(manager, VideoFile("soun"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await learner.GetAsync(Path + "?level=12")).StatusCode);

        var staged = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Storage.OnUpload = async (key, ct) =>
        {
            staged.SetResult(key);
            await resume.Task.WaitAsync(ct);
        };

        var pending = PublishAsync(manager, VideoFile());
        string key;
        Guid id;
        try
        {
            key = await staged.Task.WaitAsync(TimeSpan.FromSeconds(10));
            id = Guid.ParseExact(key["media/video-lessons/".Length..^4], "N");
            Assert.Empty((await learner.GetFromJsonAsync<VideoLessonPage>(Path))!.Items);
            Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync($"{Path}/{id}/playback")).StatusCode);
        }
        finally
        {
            resume.TrySetResult();
            factory.Storage.OnUpload = null;
        }

        using var published = await pending;
        Assert.Equal(HttpStatusCode.Created, published.StatusCode);
        Assert.Equal(id, await published.Content.ReadFromJsonAsync<Guid>());
        var listed = Assert.Single((await learner.GetFromJsonAsync<VideoLessonPage>(Path + "?level=4"))!.Items);
        Assert.Equal(new VideoLessonItem(id, "Bài thử", "Mô tả bài học", JlptLevel.N4), listed);
        Assert.Empty((await learner.GetFromJsonAsync<VideoLessonPage>(Path + "?level=5"))!.Items);

        using var playback = await learner.GetAsync($"{Path}/{id}/playback");
        playback.EnsureSuccessStatusCode();
        Assert.True(playback.Headers.CacheControl?.NoStore);
        var url = (await playback.Content.ReadFromJsonAsync<VideoLessonPlayback>())!;
        Assert.Equal($"https://media.example.invalid/{key}", url.Url);
        Assert.InRange(url.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(28), DateTimeOffset.UtcNow.AddMinutes(31));

        Assert.Equal(HttpStatusCode.Forbidden, (await learner.DeleteAsync($"{Path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync($"{Path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync($"{Path}/{id}/playback")).StatusCode);
        Assert.Empty((await learner.GetFromJsonAsync<VideoLessonPage>(Path))!.Items);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var retired = await db.VideoLessons.SingleAsync();
        Assert.Null(retired.PublishedAt);
        Assert.True(retired.CleanupAfter >= url.ExpiresAt);
    }

    private static async Task<HttpResponseMessage> PublishAsync(HttpClient client, byte[] bytes)
    {
        using var body = new MultipartFormDataContent();
        body.Add(new StringContent(" Bài thử "), "title");
        body.Add(new StringContent(" Mô tả bài học "), "description");
        body.Add(new StringContent("4"), "level");
        body.Add(new ByteArrayContent(bytes), "file", "sample.mp4");
        return await client.PostAsync(Path, body);
    }

    private static byte[] VideoFile(string handler = "vide") =>
        [.. Box("ftyp", "isom\0\0\0\0"u8.ToArray()),
         .. Box("moov", Box("trak", Box("mdia", Box("hdlr", [.. new byte[8], .. System.Text.Encoding.ASCII.GetBytes(handler)])))),
         .. Box("mdat", [1, 2, 3])];

    private static byte[] Box(string type, byte[] content)
    {
        var header = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header, header.Length + content.Length);
        System.Text.Encoding.ASCII.GetBytes(type, header.AsSpan(4));
        return [.. header, .. content];
    }
}

public sealed class VideoLessonWebApplicationFactory : CustomWebApplicationFactory
{
    public VideoStorage Storage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSingleton<IMediaObjectStorage>(Storage));
    }

    public sealed class VideoStorage : IMediaObjectStorage
    {
        public Func<string, CancellationToken, Task>? OnUpload { get; set; }
        public bool IsConfigured => true;

        public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct)
        {
            Assert.Equal("video/mp4", contentType);
            Assert.Equal(0, stream.Position);
            if (OnUpload is { } upload) await upload(key, ct);
        }

        public Task<string> SignReadAsync(string key, DateTimeOffset expiresAt, CancellationToken ct) =>
            Task.FromResult($"https://media.example.invalid/{key}");

        public Task DeleteAsync(string key, CancellationToken ct) => Task.CompletedTask;
    }
}
