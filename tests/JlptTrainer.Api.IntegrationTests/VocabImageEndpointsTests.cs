using System.Net;
using System.Net.Http.Json;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Application.Vocabs.Images;
using JlptTrainer.Application.Vocabs.Queries.GetVocabList;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class VocabImageEndpointsTests(VocabImageTestFactory factory) : IClassFixture<VocabImageTestFactory>
{
    [Fact]
    public async Task ManagersReplaceAndRemoveImagesWithoutChangingLearnerCards()
    {
        using var manager = factory.CreateClient();
        using var learner = factory.CreateClient();
        using var guest = factory.CreateClient();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        await learner.AuthenticateAsync();
        var vocabId = await CreateVocabAsync(manager);
        var vocab = $"/api/vocabs/{vocabId}";
        var image = vocab + "/image";
        byte[] png = MakePng();

        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync(image)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await learner.GetAsync(image)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PutImageAsync(learner, image, png)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await learner.DeleteAsync(image)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PutImageAsync(manager, image, [1, 2, 3])).StatusCode);
        Assert.Empty(await GetImagesAsync(vocabId));

        (await learner.PostAsJsonAsync("/api/reviewcards", new AddToReviewCommand(CardType.Vocab, vocabId))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await PutImageAsync(manager, image, png)).StatusCode);
        string firstImage = (await RequireImageAsync(learner, image)).Url;
        Assert.True((await learner.GetFromJsonAsync<VocabDto>(vocab))!.HasImage);

        var replacements = await Task.WhenAll(PutImageAsync(manager, image, png), PutImageAsync(manager, image, png));
        Assert.All(replacements, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
        var ids = await GetImagesAsync(vocabId);
        Assert.Equal(3, ids.Count);
        Assert.Single(ids, row => row.VocabId == vocabId);
        Assert.NotEqual(firstImage, (await RequireImageAsync(learner, image)).Url);
        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync(image)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await learner.GetAsync(image)).StatusCode);
        Assert.All(await GetImagesAsync(vocabId), row => Assert.Null(row.VocabId));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.ReviewCards.CountAsync(card => card.ReferenceId == vocabId));
    }

    [Fact]
    public async Task DeletingVocabDuringReplacementLeavesBothObjectsEligibleForCleanup()
    {
        using var manager = factory.CreateClient();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        var vocabId = await CreateVocabAsync(manager);
        var vocab = $"/api/vocabs/{vocabId}";
        var image = vocab + "/image";
        byte[] png = MakePng();
        Assert.Equal(HttpStatusCode.NoContent, (await PutImageAsync(manager, image, png)).StatusCode);

        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Storage.OnUpload = async ct =>
        {
            entered.SetResult();
            await hold.Task.WaitAsync(ct);
        };

        var replacement = PutImageAsync(manager, image, png);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync(vocab)).StatusCode);
        }
        finally
        {
            hold.TrySetResult();
            factory.Storage.OnUpload = null;
        }

        Assert.Equal(HttpStatusCode.NotFound, (await replacement).StatusCode);
        var images = await GetImagesAsync(vocabId);
        Assert.Equal(2, images.Count);
        Assert.All(images, row => Assert.Null(row.VocabId));
    }

    private static byte[] MakePng()
    {
        using var bitmap = new SKBitmap(8, 8);
        bitmap.Erase(SKColors.Crimson);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static async Task<Guid> CreateVocabAsync(HttpClient manager)
    {
        using var result = await manager.PostAsJsonAsync("/api/vocabs",
            new CreateVocabCommand($"写真{Guid.NewGuid():N}", "しゃしん", "ảnh", null, null, JlptLevel.N5));
        result.EnsureSuccessStatusCode();
        return await result.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task<HttpResponseMessage> PutImageAsync(HttpClient client, string path, byte[] bytes)
    {
        using var body = new MultipartFormDataContent();
        body.Add(new ByteArrayContent(bytes), "file", "sample.png");
        return await client.PutAsync(path, body);
    }

    private static async Task<VocabImageLink> RequireImageAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        return (await response.Content.ReadFromJsonAsync<VocabImageLink>())!;
    }

    private async Task<List<VocabImage>> GetImagesAsync(Guid vocabId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Set<VocabImage>().Where(row => row.ObjectKey.StartsWith($"media/vocab-images/{vocabId:N}/"))
            .ToListAsync();
    }
}

public sealed class VocabImageTestFactory : CustomWebApplicationFactory
{
    public TestMediaStorage Storage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSingleton<IMediaObjectStorage>(Storage));
    }

    public sealed class TestMediaStorage : IMediaObjectStorage
    {
        public Func<CancellationToken, Task>? OnUpload { get; set; }
        public bool IsConfigured => true;

        public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct)
        {
            using var codec = SKCodec.Create(stream);
            Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
            Assert.Equal("image/webp", contentType);
            if (OnUpload is { } upload) await upload(ct);
        }

        public Task<string> SignReadAsync(string key, DateTimeOffset expiresAt, CancellationToken ct) =>
            Task.FromResult($"https://media.example.invalid/{key}");

        public Task DeleteAsync(string key, CancellationToken ct) => Task.CompletedTask;
    }
}
