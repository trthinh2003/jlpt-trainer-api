using FluentAssertions;
using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
using JlptTrainer.Application.ReviewCards.Commands.SubmitReview;
using JlptTrainer.Application.ReviewCards.Queries.GetDueCards;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace JlptTrainer.Api.IntegrationTests
{
    public class ReviewCardEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

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

        private async Task<Guid> SeedVocabAsync(string word, string reading, string meaning)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var vocab = new Vocab { Word = word, Reading = reading, Meaning = meaning, Level = JlptLevel.N5 };
            db.Vocabs.Add(vocab);
            await db.SaveChangesAsync();
            return vocab.Id;
        }
    }
}
