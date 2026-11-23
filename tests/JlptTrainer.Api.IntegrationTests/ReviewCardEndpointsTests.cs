using FluentAssertions;
using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
using JlptTrainer.Application.ReviewCards;
using JlptTrainer.Application.ReviewCards.Commands.AddReviewCardsBatch;
using JlptTrainer.Application.ReviewCards.Commands.SubmitReview;
using JlptTrainer.Application.ReviewCards.Queries.GetDueCards;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace JlptTrainer.Api.IntegrationTests
{
    public class ReviewCardEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private const string BatchPath = "/api/reviewcards/batch";

        [Fact]
        public async Task FullSrsFlow_CreateVocab_AddToReview_ShowsInDue_ThenSubmitReview()
        {
            await _client.AuthenticateAsync();

            // 1. Tạo Vocab
            var vocabId = await SeedVocabAsync("話す", "はなす", "nói");

            // 2. Thêm vào bộ ôn tập
            var addResponse = await _client.PostAsJsonAsync("/api/reviewcards",
                new AddToReviewCommand(CardType.Vocab, vocabId));
            addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var reviewCardId = await addResponse.Content.ReadFromJsonAsync<Guid>();

            // 3. Phải xuất hiện trong danh sách due (vì NextReviewDate mặc định = now)
            var dueResponse = await _client.GetAsync("/api/reviewcards/due");
            var dueCards = await dueResponse.Content.ReadFromJsonAsync<List<DueCardDto>>();
            dueCards.Should().Contain(c => c.ReviewCardId == reviewCardId);

            // 4. Nộp kết quả ôn - chọn Good, kỳ vọng NextReviewDate = ngày mai (SM-2: lần đầu, interval=1)
            var submitResponse = await _client.PostAsJsonAsync(
                $"/api/reviewcards/{reviewCardId}/review",
                new { Grade = ReviewGrade.Good });

            submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await submitResponse.Content.ReadFromJsonAsync<SubmitReviewResult>();
            result!.IntervalDays.Should().Be(1);

            // 5. Sau khi ôn xong, ko còn nằm trong danh sách due nữa (NextReviewDate đã dời sang ngày mai)
            var dueAfterResponse = await _client.GetAsync("/api/reviewcards/due");
            var dueAfter = await dueAfterResponse.Content.ReadFromJsonAsync<List<DueCardDto>>();
            dueAfter.Should().NotContain(c => c.ReviewCardId == reviewCardId);
        }

        [Fact]
        public async Task AddToReview_SameContentTwice_ShouldReturnConflict()
        {
            await _client.AuthenticateAsync();

            var vocabId = await SeedVocabAsync("読む", "よむ", "đọc");

            var firstAdd = await _client.PostAsJsonAsync("/api/reviewcards",
                new AddToReviewCommand(CardType.Vocab, vocabId));
            firstAdd.StatusCode.Should().Be(HttpStatusCode.OK);

            // thêm lần 2 cùng Vocab - test quan trọng
            var secondAdd = await _client.PostAsJsonAsync("/api/reviewcards",
                new AddToReviewCommand(CardType.Vocab, vocabId));
            secondAdd.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Theory]
        [InlineData(CardType.Vocab)]
        [InlineData(CardType.Kanji)]
        [InlineData(CardType.Grammar)]
        public async Task Batch_AddsOnlyNewCardsForTheCurrentUser(CardType cardType)
        {
            await _client.AuthenticateAsync();
            using var otherLearner = factory.CreateClient();
            await otherLearner.AuthenticateAsync();
            var existingId = await SeedContentAsync(cardType, "同");
            var newId = await SeedContentAsync(cardType, "紙");
            var missingId = Guid.NewGuid();
            var other = await AddBatchAsync(otherLearner, cardType, newId);
            Assert.Equal(new[] { newId }, other.AddedIds);

            using var single = await _client.PostAsJsonAsync("/api/reviewcards", new AddToReviewCommand(cardType, existingId));
            single.EnsureSuccessStatusCode();
            var existingCard = await single.Content.ReadFromJsonAsync<Guid>();
            using var review = await _client.PostAsJsonAsync($"/api/reviewcards/{existingCard}/review", new { Grade = ReviewGrade.Good });
            review.EnsureSuccessStatusCode();

            var result = await AddBatchAsync(_client, cardType, existingId, newId, missingId, newId);
            Assert.Equal(new[] { newId }, result.AddedIds);
            Assert.Equal(new[] { existingId }, result.AlreadyAddedIds);
            Assert.Equal(new[] { missingId }, result.MissingIds);
            var due = (await _client.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due"))!;
            var dueCard = Assert.Single(due);
            Assert.Equal(newId, dueCard.ReferenceId);
            Assert.Equal(cardType, dueCard.CardType);
            var otherDue = (await otherLearner.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due"))!;
            Assert.NotEqual(dueCard.ReviewCardId, Assert.Single(otherDue).ReviewCardId);

            var replay = await AddBatchAsync(_client, cardType, existingId, newId, missingId);
            Assert.Empty(replay.AddedIds);
            Assert.Equal(new[] { existingId, newId }, replay.AlreadyAddedIds);
            Assert.Equal(new[] { missingId }, replay.MissingIds);
            using var duplicate = await _client.PostAsJsonAsync("/api/reviewcards", new AddToReviewCommand(cardType, newId));
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            using var missing = await _client.PostAsJsonAsync("/api/reviewcards", new AddToReviewCommand(cardType, missingId));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Fact]
        public async Task Batch_RequiresLoginAndValidCardTypeAndIds()
        {
            var id = await SeedContentAsync(CardType.Kanji, "声");
            using var guest = factory.CreateClient();
            using var unauthorized = await guest.PostAsJsonAsync(BatchPath, new AddReviewCardsBatchCommand(CardType.Kanji, [id]));
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
            await _client.AuthenticateAsync();
            Guid[][] invalid = [[], [id, Guid.Empty], Enumerable.Repeat(id, 21).ToArray()];
            foreach (var ids in invalid)
            {
                using var response = await _client.PostAsJsonAsync(BatchPath, new AddReviewCardsBatchCommand(CardType.Kanji, ids));
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
            using var missingType = await _client.PostAsJsonAsync(BatchPath, new { ReferenceIds = new[] { id } });
            Assert.Equal(HttpStatusCode.BadRequest, missingType.StatusCode);
            using var invalidType = await _client.PostAsJsonAsync(BatchPath, new AddReviewCardsBatchCommand((CardType)99, [id]));
            Assert.Equal(HttpStatusCode.BadRequest, invalidType.StatusCode);
            var wrongCatalog = await AddBatchAsync(_client, CardType.Grammar, id);
            Assert.Equal(new[] { id }, wrongCatalog.MissingIds);
            Assert.Empty((await _client.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due"))!);

            using var legacy = await _client.PostAsJsonAsync("/api/reviewcards/kanji/batch", new { ReferenceIds = new[] { id } });
            legacy.EnsureSuccessStatusCode();
            var legacyResult = (await legacy.Content.ReadFromJsonAsync<ReviewCardAddition>())!;
            Assert.Equal(new[] { id }, legacyResult.AddedIds);
        }

        [Theory]
        [InlineData(CardType.Vocab)]
        [InlineData(CardType.Kanji)]
        [InlineData(CardType.Grammar)]
        public async Task Batch_OverlappingBatchesAndSingleAddStoreEachCardOnce(CardType cardType)
        {
            await _client.AuthenticateAsync();
            var first = await SeedContentAsync(cardType, "甲");
            var second = await SeedContentAsync(cardType, "乙");
            var third = await SeedContentAsync(cardType, "丙");
            var forward = AddBatchAsync(_client, cardType, first, second);
            var backward = AddBatchAsync(_client, cardType, third, second, first);
            var single = _client.PostAsJsonAsync("/api/reviewcards", new AddToReviewCommand(cardType, second));
            await Task.WhenAll(forward, backward, single);
            using var singleResponse = await single;
            Assert.Contains(singleResponse.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict });
            var batchAdded = (await forward).AddedIds.Concat((await backward).AddedIds);
            var added = singleResponse.IsSuccessStatusCode ? batchAdded.Append(second) : batchAdded;
            Assert.Equal(new[] { first, second, third }.Order(), added.Order());
            var due = (await _client.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due"))!;
            Assert.Equal(new[] { first, second, third }.Order(), due.Select(card => card.ReferenceId).Order());
        }

        [Theory]
        [InlineData(CardType.Vocab, "vocabs")]
        [InlineData(CardType.Kanji, "kanjis")]
        [InlineData(CardType.Grammar, "grammarpoints")]
        public async Task Batch_RacingCatalogDeleteLeavesHistoryAndNoOrphanCards(CardType cardType, string catalogPath)
        {
            await _client.AuthenticateAsync();
            using var manager = factory.CreateClient();
            await manager.AuthenticateAsCatalogManagerAsync(factory);
            var deletedId = await SeedContentAsync(cardType, "庁");
            var retainedId = await SeedContentAsync(cardType, "集");
            using var previous = await manager.PostAsJsonAsync("/api/reviewcards", new AddToReviewCommand(cardType, deletedId));
            previous.EnsureSuccessStatusCode();
            var previousCard = await previous.Content.ReadFromJsonAsync<Guid>();
            using var grade = await manager.PostAsJsonAsync($"/api/reviewcards/{previousCard}/review", new { Grade = ReviewGrade.Good });
            grade.EnsureSuccessStatusCode();

            var adding = AddBatchAsync(_client, cardType, deletedId, retainedId);
            var deleting = manager.DeleteAsync($"/api/{catalogPath}/{deletedId}");
            await Task.WhenAll(adding, deleting);
            var result = await adding;
            using var deletion = await deleting;
            Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
            Assert.Contains(retainedId, result.AddedIds);
            Assert.Contains(deletedId, result.AddedIds.Concat(result.MissingIds));

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await db.ReviewCards.AnyAsync(card => card.CardType == cardType && card.ReferenceId == deletedId));
            Assert.True(await db.ReviewEvents.AnyAsync(review => review.ReviewCardId == previousCard));
            var due = (await _client.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due"))!;
            Assert.Equal(retainedId, Assert.Single(due).ReferenceId);
        }

        private static async Task<ReviewCardAddition> AddBatchAsync(HttpClient client, CardType cardType, params Guid[] ids)
        {
            using var response = await client.PostAsJsonAsync(BatchPath, new AddReviewCardsBatchCommand(cardType, ids));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<ReviewCardAddition>())!;
        }

        [Fact]
        public async Task DueCards_CombinesFiltersBeforeTheLimitAndRestrictsOwnerAndDueDate()
        {
            var learner = await _client.AuthenticateAsync();
            using var otherClient = factory.CreateClient();
            var otherLearner = await otherClient.AuthenticateAsync();
            var n5Kanji = await SeedContentAsync(CardType.Kanji, "辺");
            var n4Kanji = await SeedContentAsync(CardType.Kanji, "短", JlptLevel.N4);
            var n4Vocab = await SeedContentAsync(CardType.Vocab, "帰る", JlptLevel.N4);
            var n4Grammar = await SeedContentAsync(CardType.Grammar, "〜ても", JlptLevel.N4);
            var futureKanji = await SeedContentAsync(CardType.Kanji, "消", JlptLevel.N4);
            var now = DateTimeOffset.UtcNow;
            var oldest = SrsCalculator.InitialState(now.AddDays(-3));
            var past = SrsCalculator.InitialState(now.AddDays(-2));
            var recentlyDue = SrsCalculator.InitialState(now.AddDays(-1));
            var future = SrsCalculator.InitialState(now.AddDays(1));

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ReviewCards.AddRange(
                new ReviewCard(learner.UserId, CardType.Kanji, n5Kanji, oldest),
                new ReviewCard(learner.UserId, CardType.Kanji, n4Kanji, recentlyDue),
                new ReviewCard(learner.UserId, CardType.Vocab, n4Vocab, past),
                new ReviewCard(learner.UserId, CardType.Grammar, n4Grammar, recentlyDue),
                new ReviewCard(learner.UserId, CardType.Kanji, futureKanji, future),
                new ReviewCard(otherLearner.UserId, CardType.Kanji, n4Kanji, oldest));
            await db.SaveChangesAsync();

            async Task<List<DueCardDto>> Due(string query) =>
                (await _client.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due" + query))!;

            Assert.Equal(n5Kanji, Assert.Single(await Due("?maxCards=1")).ReferenceId);
            Assert.Equal(n4Kanji, Assert.Single(await Due("?cardType=1&level=4&maxCards=1")).ReferenceId);
            Assert.Equal(n4Vocab, Assert.Single(await Due("?cardType=0")).ReferenceId);
            Assert.Equal(n4Grammar, Assert.Single(await Due("?cardType=2&level=4")).ReferenceId);
            Assert.Equal(new[] { n5Kanji, n4Kanji }, (await Due("?cardType=1")).Select(card => card.ReferenceId));
            var levelMatches = await Due("?level=4");
            Assert.Equal(n4Vocab, levelMatches[0].ReferenceId);
            Assert.Equal(new[] { n4Kanji, n4Vocab, n4Grammar }.Order(), levelMatches.Select(card => card.ReferenceId).Order());
            Assert.Empty(await Due("?cardType=2&level=5"));
        }

        [Theory]
        [InlineData("cardType=3")]
        [InlineData("level=9")]
        public async Task DueCards_RejectsInvalidFilters(string query)
        {
            await _client.AuthenticateAsync();
            using var response = await _client.GetAsync("/api/reviewcards/due?" + query);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private async Task<Guid> SeedContentAsync(CardType cardType, string title, JlptLevel level = JlptLevel.N5)
        {
            if (cardType == CardType.Vocab) return await SeedVocabAsync(title, title, "nghĩa kiểm thử", level);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var id = Guid.NewGuid();
            if (cardType == CardType.Kanji)
                db.Kanjis.Add(new Kanji { Id = id, Character = title, StrokeCount = 5, Meaning = "nghĩa kiểm thử", Level = level });
            else
                db.GrammarPoints.Add(new GrammarPoint { Id = id, Pattern = title, Meaning = "nghĩa kiểm thử", Level = level });
            await db.SaveChangesAsync();
            return id;
        }

        private async Task<Guid> SeedVocabAsync(string word, string reading, string meaning, JlptLevel level = JlptLevel.N5)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var vocab = new Vocab { Word = word, Reading = reading, Meaning = meaning, Level = level };
            db.Vocabs.Add(vocab);
            await db.SaveChangesAsync();
            return vocab.Id;
        }
    }
}
