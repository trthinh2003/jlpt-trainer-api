using FluentAssertions;
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
            await _client.AuthenticateAsync();

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
            await _client.AuthenticateAsync();

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
            await _client.AuthenticateAsync();

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
    }
}
