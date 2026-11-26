using System.Net;
using System.Text.Json;
using JlptTrainer.Application.AudioLessons.Commands.PublishAudioLesson;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class AudioLessonPublicationRaceTests(AudioPublicationRaceFactory factory)
    : IClassFixture<AudioPublicationRaceFactory>
{
    [Fact]
    public async Task DeletingAWordBeforeTheFinalSaveReturnsConflictAndLeavesTheDraftUnpublished()
    {
        using var publisher = factory.CreateClient();
        using var manager = factory.CreateClient();
        await publisher.AuthenticateAsCatalogManagerAsync(factory);
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        var word = await AudioLessonEndpointsTests.CreateWordAsync(manager, "電車", "でんしゃ", "tàu điện");

        var publishing = AudioLessonEndpointsTests.PublishAsync(publisher, "[00:00.0]電車。", [word]);
        Guid draftId;
        try
        {
            draftId = await factory.Gate.SavingLinks.Task.WaitAsync(TimeSpan.FromSeconds(15));
            using var deleted = await manager.DeleteAsync($"/api/vocabs/{word}");
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }
        finally
        {
            factory.Gate.Release.TrySetResult();
        }

        using var response = await publishing.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(PublishAudioLesson.MissingVocabMessage, body.RootElement.GetProperty("error").GetString());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Null((await db.AudioLessons.SingleAsync(lesson => lesson.Id == draftId)).PublishedAt);
        Assert.False(await db.Set<AudioLessonWord>().AnyAsync(link => link.AudioLessonId == draftId));
        using var playback = await manager.GetAsync($"/api/audio-lessons/{draftId}/playback");
        Assert.Equal(HttpStatusCode.NotFound, playback.StatusCode);
    }
}

public sealed class AudioPublicationRaceFactory : CustomWebApplicationFactory
{
    internal SaveGate Gate { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IMediaObjectStorage, AudioLessonWebApplicationFactory.AudioStorage>();
            services.ConfigureDbContext<ApplicationDbContext>(options => options.AddInterceptors(Gate));
        });
    }

    internal sealed class SaveGate : SaveChangesInterceptor
    {
        public TaskCompletionSource<Guid> SavingLinks { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var link = eventData.Context?.ChangeTracker.Entries<AudioLessonWord>()
                .FirstOrDefault(entry => entry.State == EntityState.Added);
            if (link is not null)
            {
                SavingLinks.TrySetResult(link.Entity.AudioLessonId);
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }
}
