using FluentAssertions;
using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
using JlptTrainer.Application.ReviewCards.Queries.GetDueCards;
using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Application.Vocabs.Queries.GetVocabList;
using JlptTrainer.Domain.Enums;
using System.Net;
using System.Net.Http.Json;

namespace JlptTrainer.Api.IntegrationTests
{
    public class VocabEndpointsTests(CustomWebApplicationFactory factory)
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task Create_WithoutAuth_ShouldReturnUnauthorized()
        {
            var command = new CreateVocabCommand("食べる", "たべる", "ăn", null, null, JlptLevel.N5);

            var response = await _client.PostAsJsonAsync("/api/vocabs", command);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_WithValidData_ShouldReturnCreatedAndBeRetrievable()
        {
            await _client.AuthenticateAsCatalogManagerAsync(factory);

            var command = new CreateVocabCommand("飲む", "のむ", "uống", "水を飲む", "Uống nước", JlptLevel.N5);
            var createResponse = await _client.PostAsJsonAsync("/api/vocabs", command);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var vocabId = await createResponse.Content.ReadFromJsonAsync<Guid>();
            vocabId.Should().NotBeEmpty();

            var getResponse = await _client.GetAsync($"/api/vocabs/{vocabId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var vocab = await getResponse.Content.ReadFromJsonAsync<VocabDto>();
            vocab!.Word.Should().Be("飲む");
            vocab.Meaning.Should().Be("uống");
        }

        [Fact]
        public async Task Delete_ShouldRemoveVocab_AndReturnNotFoundOnSubsequentGet()
        {
            await _client.AuthenticateAsCatalogManagerAsync(factory);

            var command = new CreateVocabCommand("書く", "かく", "viết", null, null, JlptLevel.N5);
            var createResponse = await _client.PostAsJsonAsync("/api/vocabs", command);
            var vocabId = await createResponse.Content.ReadFromJsonAsync<Guid>();

            var deleteResponse = await _client.DeleteAsync($"/api/vocabs/{vocabId}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/vocabs/{vocabId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetList_ShouldFilterByLevel()
        {
            await _client.AuthenticateAsCatalogManagerAsync(factory);

            await _client.PostAsJsonAsync("/api/vocabs",
                new CreateVocabCommand("N5word", "n5reading", "n5 meaning", null, null, JlptLevel.N5));
            await _client.PostAsJsonAsync("/api/vocabs",
                new CreateVocabCommand("N3word", "n3reading", "n3 meaning", null, null, JlptLevel.N3));

            var response = await _client.GetAsync("/api/vocabs?level=5"); // N5 = 5
            var result = await response.Content.ReadFromJsonAsync<PagedVocabResult>();

            result!.Items.Should().OnlyContain(v => v.Level == JlptLevel.N5);
            result.Items.Should().Contain(v => v.Word == "N5word");
            result.Items.Should().NotContain(v => v.Word == "N3word");
        }

        [Fact]
        public async Task GetList_ShouldSearchWordOrReading_WithinLevelAndAcrossPages()
        {
            await _client.AuthenticateAsCatalogManagerAsync(factory);
            var entries = new[]
            {
                new CreateVocabCommand("案内係", "あんないがかり", "hướng dẫn viên", null, null, JlptLevel.N4),
                new CreateVocabCommand("係員", "あんないスタッフ", "nhân viên", null, null, JlptLevel.N4),
                new CreateVocabCommand("案内", "あんない", "hướng dẫn", null, null, JlptLevel.N3),
                new CreateVocabCommand("係", "かかり", "người phụ trách", null, null, JlptLevel.N4),
                new CreateVocabCommand("Book_100%", "ぶっく", "sách", null, null, JlptLevel.N4),
            };
            foreach (var entry in entries)
            {
                (await _client.PostAsJsonAsync("/api/vocabs", entry)).EnsureSuccessStatusCode();
            }

            using var learner = factory.CreateClient();
            await learner.AuthenticateAsync();
            var word = await learner.GetFromJsonAsync<PagedVocabResult>(
                $"/api/vocabs?level=4&search={Uri.EscapeDataString(" 案内 ")}");
            word!.TotalCount.Should().Be(1);
            word.Items.Should().ContainSingle().Which.Word.Should().Be("案内係");

            var readingUrl = $"/api/vocabs?level=4&pageSize=1&search={Uri.EscapeDataString("あんない")}";
            var first = (await learner.GetFromJsonAsync<PagedVocabResult>($"{readingUrl}&pageNumber=1"))!;
            var second = (await learner.GetFromJsonAsync<PagedVocabResult>($"{readingUrl}&pageNumber=2"))!;
            first.TotalCount.Should().Be(2);
            first.TotalPages.Should().Be(2);
            first.Items.Concat(second.Items).Select(item => item.Word)
                .Should().BeEquivalentTo(new[] { "案内係", "係員" });
            var outOfRange = await learner.GetFromJsonAsync<PagedVocabResult>(
                $"/api/vocabs?level=4&pageSize=100&pageNumber={int.MaxValue}");
            outOfRange!.Items.Should().BeEmpty();

            var literal = (await learner.GetFromJsonAsync<PagedVocabResult>(
                $"/api/vocabs?search={Uri.EscapeDataString("BOOK_100%")}"))!;
            literal.Items.Should().ContainSingle().Which.Word.Should().Be("Book_100%");
            var underscore = (await learner.GetFromJsonAsync<PagedVocabResult>("/api/vocabs?search=_"))!;
            underscore.Items.Should().ContainSingle().Which.Word.Should().Be("Book_100%");
            (await learner.GetAsync($"/api/vocabs?search={new string('x', 101)}"))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateContent_ShouldRequireManagerAndPreserveLearnersReviewCard()
        {
            using var guest = factory.CreateClient();
            using var learner = factory.CreateClient();
            await learner.AuthenticateAsync();
            await _client.AuthenticateAsCatalogManagerAsync(factory);
            var create = new CreateVocabCommand("読む", "よむ", "read", "本を読む", null, JlptLevel.N5);
            var created = await _client.PostAsJsonAsync("/api/vocabs", create);
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            var id = await created.Content.ReadFromJsonAsync<Guid>();
            var detailsUrl = $"/api/vocabs/{id}";
            var contentUrl = $"{detailsUrl}/content";

            (await learner.PostAsJsonAsync("/api/reviewcards", new AddToReviewCommand(CardType.Vocab, id)))
                .EnsureSuccessStatusCode();
            var dueBefore = (await learner.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due"))!
                .Single(card => card.ReferenceId == id);

            var edit = new { Meaning = "  đọc  ", ExampleSentence = "  新聞を読む  ", ExampleSentenceMeaning = "  Đọc báo  " };
            (await guest.PutAsJsonAsync(contentUrl, edit)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await learner.PutAsJsonAsync(contentUrl, edit)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await _client.PutAsJsonAsync(contentUrl, new { Meaning = "  " })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await _client.PutAsJsonAsync(contentUrl, new { Meaning = new string('x', 501) }))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await _client.PutAsJsonAsync($"/api/vocabs/{Guid.NewGuid()}/content", edit))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await learner.GetFromJsonAsync<VocabDto>(detailsUrl))!.Meaning.Should().Be(create.Meaning);

            (await _client.PutAsJsonAsync(contentUrl, edit)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await learner.GetFromJsonAsync<VocabDto>(detailsUrl)).Should().Be(
                new VocabDto(id, create.Word, create.Reading, "đọc", "新聞を読む", "Đọc báo", create.Level));
            var dueAfter = (await learner.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due"))!
                .Single(card => card.ReferenceId == id);
            dueAfter.Should().Be(dueBefore);

            (await _client.PutAsJsonAsync(contentUrl, new { Meaning = "đọc", ExampleSentence = " " }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
            var cleared = (await learner.GetFromJsonAsync<VocabDto>(detailsUrl))!;
            cleared.ExampleSentence.Should().BeNull();
            cleared.ExampleSentenceMeaning.Should().BeNull();
        }
    }
}
