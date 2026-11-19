using System.Net;
using System.Net.Http.Json;
using JlptTrainer.Application.Dashboard.Queries.GetProgressStats;
using JlptTrainer.Application.Dashboard.Queries.GetStudyHeatmap;
using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
using JlptTrainer.Application.ReviewCards.Commands.SubmitReview;
using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class DashboardEndpointsTests(DashboardWebApplicationFactory factory)
    : IClassFixture<DashboardWebApplicationFactory>
{
    [Fact]
    public async Task Dashboard_GroupsReviewsByUtcDayAndKeepsHistoryAfterCardDeletion()
    {
        var lastDay = new DateTimeOffset(2026, 11, 18, 23, 59, 59, TimeSpan.Zero).AddTicks(7);
        factory.Clock.Now = lastDay;
        using var learner = factory.CreateClient();
        using var manager = factory.CreateClient();
        using var other = factory.CreateClient();
        var learnerAccount = await learner.AuthenticateAsync();
        var otherAccount = await other.AuthenticateAsync();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        var vocabId = await CreateVocabAsync(manager);
        var cardId = await AddCardAsync(learner, vocabId);
        var firstRequest = Guid.NewGuid();
        var first = await GradeAsync(learner, cardId, firstRequest);

        factory.Clock.Now = new DateTimeOffset(2026, 11, 19, 0, 0, 0, TimeSpan.Zero);
        await GradeAsync(learner, cardId, Guid.NewGuid());
        await GradeAsync(learner, cardId, Guid.NewGuid());
        Assert.Equal(first, await GradeAsync(learner, cardId, firstRequest));

        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync(
            $"/api/reviewcards/{cardId}/review", new { grade = ReviewGrade.Good, requestId = firstRequest })).StatusCode);
        var heatmap = await learner.GetFromJsonAsync<List<HeatmapEntryDto>>("/api/dashboard/heatmap?days=2");
        Assert.Collection(heatmap!,
            day => Assert.Equal(new HeatmapEntryDto(new DateOnly(2026, 11, 18), 1), day),
            day => Assert.Equal(new HeatmapEntryDto(new DateOnly(2026, 11, 19), 2), day));
        var today = await learner.GetFromJsonAsync<List<HeatmapEntryDto>>("/api/dashboard/heatmap?days=1");
        Assert.Equal(new HeatmapEntryDto(new DateOnly(2026, 11, 19), 2), Assert.Single(today!));
        Assert.Empty((await other.GetFromJsonAsync<List<HeatmapEntryDto>>("/api/dashboard/heatmap?days=2"))!);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.MockTests.AddRange(
            MockResult(learnerAccount.UserId, correct: 5),
            MockResult(learnerAccount.UserId, correct: 10),
            MockResult(otherAccount.UserId, correct: 0));
        await db.SaveChangesAsync();

        var progress = await learner.GetFromJsonAsync<ProgressStatsResult>("/api/dashboard/progress");
        Assert.NotNull(progress);
        Assert.Equal(new CardTypeProgressDto(CardType.Vocab, 1, 1), Assert.Single(progress.CardProgress));
        Assert.Equal(new MockTestTrendDto(new DateOnly(2026, 11, 19), JlptLevel.N5, 75, 2), Assert.Single(progress.MockTestTrend));

        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync($"/api/vocabs/{vocabId}")).StatusCode);
        var afterDeletion = await learner.GetFromJsonAsync<List<HeatmapEntryDto>>("/api/dashboard/heatmap?days=2");
        Assert.Equal(heatmap!, afterDeletion!);
    }

    [Fact]
    public async Task ConcurrentRetries_WithSameRequestId_RecordOnlyOneReview()
    {
        factory.Clock.Now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        using var learner = factory.CreateClient();
        using var manager = factory.CreateClient();
        await learner.AuthenticateAsync();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        var cardId = await AddCardAsync(learner, await CreateVocabAsync(manager));
        var requestId = Guid.NewGuid();

        var results = await Task.WhenAll(GradeAsync(learner, cardId, requestId), GradeAsync(learner, cardId, requestId));
        Assert.Equal(results[0], results[1]);
        var duplicateWithDifferentGrade = await learner.PostAsJsonAsync(
            $"/api/reviewcards/{cardId}/review", new { grade = ReviewGrade.Again, requestId });
        Assert.Equal(HttpStatusCode.Conflict, duplicateWithDifferentGrade.StatusCode);

        var heatmap = await learner.GetFromJsonAsync<List<HeatmapEntryDto>>("/api/dashboard/heatmap?days=1");
        Assert.Equal(1, Assert.Single(heatmap!).ReviewCount);
    }

    [Fact]
    public async Task Migration_PreservesLastReviewFromLegacyCard()
    {
        factory.Clock.Now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        using var learner = factory.CreateClient();
        var learnerAccount = await learner.AuthenticateAsync();
        var reviewId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var reviewedAt = factory.Clock.GetUtcNow().AddDays(-1);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync("20261119140735_AddUserRole");
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO review_cards (id, user_id, card_type, reference_id, ease_factor, interval_days,
                                          repetitions, next_review_date, last_reviewed_at, created_at)
                VALUES ({reviewId}, {learnerAccount.UserId}, {(int)CardType.Vocab}, {contentId}, 2.5, 1,
                        1, {reviewedAt.AddDays(1)}, {reviewedAt}, {reviewedAt});
                """);
        }
        finally
        {
            await migrator.MigrateAsync();
        }

        var heatmap = await learner.GetFromJsonAsync<List<HeatmapEntryDto>>("/api/dashboard/heatmap?days=2");
        Assert.Equal(new HeatmapEntryDto(new DateOnly(2026, 11, 19), 1), Assert.Single(heatmap!));
        var backfill = await db.ReviewEvents.SingleAsync(review => review.ReviewCardId == reviewId);
        Assert.Null(backfill.Grade);
    }

    private MockTest MockResult(Guid userId, int correct) => new()
    {
        UserId = userId,
        Level = JlptLevel.N5,
        TotalQuestions = 10,
        CorrectAnswers = correct,
        Duration = TimeSpan.FromMinutes(1),
        TakenAt = factory.Clock.GetUtcNow(),
    };

    private static async Task<Guid> CreateVocabAsync(HttpClient manager)
    {
        var response = await manager.PostAsJsonAsync("/api/vocabs",
            new CreateVocabCommand("話す", "はなす", "nói", null, null, JlptLevel.N5));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task<Guid> AddCardAsync(HttpClient learner, Guid vocabId)
    {
        var response = await learner.PostAsJsonAsync("/api/reviewcards", new AddToReviewCommand(CardType.Vocab, vocabId));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task<SubmitReviewResult> GradeAsync(HttpClient learner, Guid cardId, Guid requestId)
    {
        var response = await learner.PostAsJsonAsync($"/api/reviewcards/{cardId}/review", new { grade = ReviewGrade.Good, requestId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubmitReviewResult>())!;
    }
}

public sealed class DashboardWebApplicationFactory : CustomWebApplicationFactory
{
    public DashboardTestClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }
}

public sealed class DashboardTestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => Now;
}
