using System.Net;
using System.Net.Http.Json;
using System.Text;
using JlptTrainer.Application.MockTests;
using JlptTrainer.Application.MockTests.Queries.GetMockTestHistory;
using JlptTrainer.Application.MockTests.Queries.GetMockTestReview;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class MockTestAttemptEndpointsTests(ClockWebApplicationFactory factory) : IClassFixture<ClockWebApplicationFactory>
{
    [Fact]
    public async Task Submit_UsesFixedQuestionsAfterCatalogDeletion_AndRecordsOneResult()
    {
        factory.Clock.Now = new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero);
        using var learner = factory.CreateClient();
        using var other = factory.CreateClient();
        using var manager = factory.CreateClient();
        await learner.AuthenticateAsync();
        await other.AuthenticateAsync();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        var vocab = await SeedVocabularyAsync(JlptLevel.N4, "nói", "nghe", "đọc", "viết");

        var attempt = await StartAsync(learner, JlptLevel.N4);
        var attemptUrl = $"/api/mocktests/attempts/{attempt.Id}";
        var submitUrl = $"{attemptUrl}/submit";
        Assert.Equal(TimeSpan.FromMinutes(15), attempt.ExpiresAt - attempt.ServerTime);
        Assert.Null(attempt.ResultId);
        Assert.Equal(4, attempt.Questions.Count);
        Assert.All(attempt.Questions, question =>
            Assert.Equal(4, question.Choices.Distinct(StringComparer.OrdinalIgnoreCase).Count()));
        using var resumed = await learner.GetAsync(attemptUrl);
        Assert.DoesNotContain("correctChoiceIndex", await resumed.Content.ReadAsStringAsync());
        var restored = (await resumed.Content.ReadFromJsonAsync<MockTestAttemptDto>())!;
        Assert.Equal(attempt.Questions.Select(q => (q.Word, string.Join('|', q.Choices))),
            restored.Questions.Select(q => (q.Word, string.Join('|', q.Choices))));
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(attemptUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await other.PostAsJsonAsync(submitUrl, new { selectedChoices = new int?[4] })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await learner.PostAsJsonAsync(submitUrl, new { selectedChoices = new int?[] { 4, null, null, null } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await learner.PostAsJsonAsync(submitUrl, new { selectedChoices = new int?[] { 0 } })).StatusCode);

        var first = attempt.Questions[0];
        var source = vocab.Single(entry => entry.Word == first.Word);
        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync($"/api/vocabs/{source.Id}")).StatusCode);
        var available = vocab.Single(entry => entry.Word == attempt.Questions[1].Word);
        var removed = vocab.Single(entry => entry.Word == attempt.Questions[2].Word);
        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync($"/api/vocabs/{removed.Id}")).StatusCode);
        var right = Array.IndexOf(first.Choices, source.Meaning);
        Assert.InRange(right, 0, 3);
        var selections = new int?[] { right, null, null, null };
        factory.Clock.Now = factory.Clock.Now.AddMinutes(2);
        var submitted = await Task.WhenAll(SubmitAsync(learner, submitUrl, selections), SubmitAsync(learner, submitUrl, selections));
        Assert.Equal(submitted[0], submitted[1]);

        factory.Clock.Now = attempt.ExpiresAt.AddMinutes(1);
        Assert.Equal(submitted[0], await SubmitAsync(learner, submitUrl, selections));
        Assert.Equal(HttpStatusCode.Conflict,
            (await learner.PostAsJsonAsync(submitUrl, new { selectedChoices = new int?[4] })).StatusCode);
        var resultUrl = $"/api/mocktests/{submitted[0]}";
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(resultUrl)).StatusCode);
        var review = (await learner.GetFromJsonAsync<MockTestReviewDto>(resultUrl))!;
        Assert.Equal(1, review.CorrectAnswers);
        Assert.Equal(25, review.ScorePercentage);
        Assert.Equal(TimeSpan.FromMinutes(2), review.Duration);
        Assert.Equal(first.Word, review.Questions![0].Word);
        Assert.Equal(right, review.Questions[0].CorrectChoiceIndex);
        Assert.Null(review.Questions[0].ReviewVocabId);
        Assert.Null(review.Questions[1].SelectedChoiceIndex);
        Assert.Equal(available.Id, review.Questions[1].ReviewVocabId);
        Assert.Null(review.Questions[2].ReviewVocabId);
        var add = new { cardType = CardType.Vocab, referenceId = available.Id };
        var additions = await Task.WhenAll(
            learner.PostAsJsonAsync("/api/reviewcards", add), learner.PostAsJsonAsync("/api/reviewcards", add));
        Assert.Contains(additions, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Contains(additions, response => response.StatusCode == HttpStatusCode.Conflict);
        var history = (await learner.GetFromJsonAsync<PagedMockTestHistoryResult>("/api/mocktests/history"))!;
        Assert.Equal(review.Id, Assert.Single(history.Items).Id);
        Assert.Equal(review.Id, (await learner.GetFromJsonAsync<MockTestAttemptDto>(attemptUrl))!.ResultId);
        await AssertPdfAsync(learner, resultUrl);
    }

    [Fact]
    public async Task ExpiredAttempt_CannotWrite_AndLegacyResultStillHasHistoryAndPdf()
    {
        factory.Clock.Now = new DateTimeOffset(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);
        using var learner = factory.CreateClient();
        using var other = factory.CreateClient();
        var account = await learner.AuthenticateAsync();
        await other.AuthenticateAsync();
        await SeedVocabularyAsync(JlptLevel.N3, "một", "hai", "ba", "bốn");
        var attempt = await StartAsync(learner, JlptLevel.N3);

        factory.Clock.Now = attempt.ExpiresAt;
        Assert.Equal(HttpStatusCode.Gone, (await learner.PostAsJsonAsync(
            $"/api/mocktests/attempts/{attempt.Id}/submit", new { selectedChoices = new int?[4] })).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await learner.PostAsJsonAsync("/api/mocktests/submit", new { })).StatusCode);
        Assert.Null((await learner.GetFromJsonAsync<MockTestAttemptDto>($"/api/mocktests/attempts/{attempt.Id}"))!.ResultId);
        Assert.Equal(HttpStatusCode.BadRequest, (await learner.GetAsync("/api/mocktests/history?pageSize=0")).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var legacy = new MockTest
        {
            UserId = account.UserId, Level = JlptLevel.N5, TotalQuestions = 3, CorrectAnswers = 1,
            Duration = TimeSpan.FromMinutes(1), TakenAt = factory.Clock.Now,
        };
        db.MockTests.Add(legacy);
        await db.SaveChangesAsync();

        var history = (await learner.GetFromJsonAsync<PagedMockTestHistoryResult>("/api/mocktests/history"))!;
        Assert.Equal(33.33, Assert.Single(history.Items).ScorePercentage);
        var legacyUrl = $"/api/mocktests/{legacy.Id}";
        var review = (await learner.GetFromJsonAsync<MockTestReviewDto>(legacyUrl))!;
        Assert.Null(review.Questions);
        Assert.Equal(legacy.ScorePercentage, review.ScorePercentage);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"{legacyUrl}/pdf")).StatusCode);
        await AssertPdfAsync(learner, legacyUrl);
    }

    [Fact]
    public async Task Start_RequiresFourDistinctMeaningsAtRequestedLevel()
    {
        using var learner = factory.CreateClient();
        await learner.AuthenticateAsync();
        await SeedVocabularyAsync(JlptLevel.N1, " learn", "Learn ", "study", "read");
        var response = await learner.PostAsJsonAsync("/api/mocktests/attempts", new { level = JlptLevel.N1, questionCount = 4 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private async Task<Vocab[]> SeedVocabularyAsync(JlptLevel level, params string[] meanings)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var vocab = meanings.Select((meaning, index) => new Vocab
        {
            Level = level, Word = $"{level}語{index}", Reading = $"ご{index}", Meaning = meaning,
        }).ToArray();
        db.Vocabs.AddRange(vocab);
        await db.SaveChangesAsync();
        return vocab;
    }

    private static async Task<MockTestAttemptDto> StartAsync(HttpClient client, JlptLevel level)
    {
        using var response = await client.PostAsJsonAsync("/api/mocktests/attempts", new { level, questionCount = 4 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MockTestAttemptDto>())!;
    }

    private static async Task<Guid> SubmitAsync(HttpClient client, string url, int?[] selectedChoices)
    {
        using var response = await client.PostAsJsonAsync(url, new { selectedChoices });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task AssertPdfAsync(HttpClient client, string resultUrl)
    {
        using var response = await client.GetAsync($"{resultUrl}/pdf");
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var pdf = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
    }
}
