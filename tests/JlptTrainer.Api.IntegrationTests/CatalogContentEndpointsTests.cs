using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JlptTrainer.Application.GrammarPoints.Commands.CreateGrammarPoint;
using JlptTrainer.Application.Kanjis.Commands.CreateKanji;
using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
using JlptTrainer.Application.ReviewCards.Queries.GetDueCards;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class CatalogContentEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Theory]
    [InlineData(CardType.Kanji)]
    [InlineData(CardType.Grammar)]
    public async Task CatalogContent_SearchAndEditPreserveLearnerCard(CardType cardType)
    {
        using var learner = factory.CreateClient();
        using var manager = factory.CreateClient();
        await learner.AuthenticateAsync();
        await manager.AuthenticateAsCatalogManagerAsync(factory);

        var isKanji = cardType == CardType.Kanji;
        var path = isKanji ? "/api/kanjis" : "/api/grammarpoints";
        const string originalMeaning = "một cách nói";
        object create = isKanji
            ? new CreateKanjiCommand("雨", "ウ", "あめ", originalMeaning, 8, JlptLevel.N5)
            : new CreateGrammarPointCommand("〜ても", originalMeaning, "雨でも", null, JlptLevel.N5);
        using var created = await manager.PostAsJsonAsync(path, create);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = await created.Content.ReadFromJsonAsync<Guid>();
        var detailsUrl = $"{path}/{id}";
        var editUrl = $"{detailsUrl}/content";
        object unrelated = isKanji
            ? new CreateKanjiCommand("木", "モク", "き", "cây", 4, JlptLevel.N5)
            : new CreateGrammarPointCommand("〜ないで", "đừng", null, null, JlptLevel.N5);
        using var distractor = await manager.PostAsJsonAsync(path, unrelated);
        Assert.Equal(HttpStatusCode.Created, distractor.StatusCode);

        foreach (var search in isKanji ? new[] { "雨", "ウ", "あめ", " MỘT CÁCH " } : new[] { "ても", " MỘT CÁCH " })
        {
            var query = Uri.EscapeDataString(search);
            var matching = await learner.GetFromJsonAsync<JsonElement>($"{path}?search={query}&level=5&pageSize=1");
            Assert.Equal(1, matching.GetProperty("totalCount").GetInt32());
            Assert.Equal(id, Assert.Single(matching.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
            var otherLevel = await learner.GetFromJsonAsync<JsonElement>($"{path}?search={query}&level=4");
            Assert.Equal(0, otherLevel.GetProperty("totalCount").GetInt32());
        }

        (await learner.PostAsJsonAsync("/api/reviewcards", new AddToReviewCommand(cardType, id))).EnsureSuccessStatusCode();
        var dueBefore = Assert.Single((await learner.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due"))!);
        object update = isKanji
            ? new { Meaning = "  mưa  ", OnYomi = "  ウ  ", KunYomi = " " }
            : new { Meaning = "  cho dù  ", ExampleSentence = "  雨が降っても行く  ", ExampleSentenceMeaning = " " };

        Assert.Equal(HttpStatusCode.Forbidden, (await learner.PutAsJsonAsync(editUrl, update)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PutAsJsonAsync(editUrl, new { Meaning = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await manager.PutAsJsonAsync($"{path}/{Guid.NewGuid()}/content", update)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await manager.PutAsJsonAsync(editUrl, update)).StatusCode);

        var edited = await learner.GetFromJsonAsync<JsonElement>(detailsUrl);
        Assert.Equal(id, edited.GetProperty("id").GetGuid());
        Assert.Equal(5, edited.GetProperty("level").GetInt32());
        if (isKanji)
        {
            Assert.Equal("雨", edited.GetProperty("character").GetString());
            Assert.Equal(8, edited.GetProperty("strokeCount").GetInt32());
            Assert.Equal("mưa", edited.GetProperty("meaning").GetString());
            Assert.Equal("ウ", edited.GetProperty("onYomi").GetString());
            Assert.Equal("", edited.GetProperty("kunYomi").GetString());
        }
        else
        {
            Assert.Equal("〜ても", edited.GetProperty("pattern").GetString());
            Assert.Equal("cho dù", edited.GetProperty("meaning").GetString());
            Assert.Equal("雨が降っても行く", edited.GetProperty("exampleSentence").GetString());
            Assert.Equal(JsonValueKind.Null, edited.GetProperty("exampleSentenceMeaning").ValueKind);
        }
        var dueAfter = Assert.Single((await learner.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due"))!);
        Assert.Equal(dueBefore, dueAfter);
    }
}
