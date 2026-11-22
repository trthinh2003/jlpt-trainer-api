using System.Net;
using System.Net.Http.Json;
using JlptTrainer.Api.Controllers;
using JlptTrainer.Application.GrammarPoints.Commands.CreateGrammarPoint;
using JlptTrainer.Application.Kanjis.Commands.CreateKanji;
using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
using JlptTrainer.Application.ReviewCards.Queries.GetDueCards;
using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class CatalogAuthorizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Theory]
    [InlineData(CardType.Vocab)]
    [InlineData(CardType.Kanji)]
    [InlineData(CardType.Grammar)]
    public async Task CatalogManagement_RequiresManagerForCreateImportPreviewImportAndDelete(CardType cardType)
    {
        var (path, command) = CatalogEntry(cardType);
        using var guest = factory.CreateClient();
        using var learner = factory.CreateClient();
        using var manager = factory.CreateClient();
        await learner.AuthenticateAsync();
        await manager.AuthenticateAsCatalogManagerAsync(factory);

        var create = await manager.PostAsJsonAsync(path, command);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = await create.Content.ReadFromJsonAsync<Guid>();
        var detailsPath = $"{path}/{id}";
        var workbook = await manager.GetByteArrayAsync($"{path}/import/template");

        Assert.Equal(HttpStatusCode.OK, (await learner.GetAsync(detailsPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsJsonAsync(path, command)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ImportAsync(guest, path, workbook)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ImportAsync(guest, path, workbook, preview: true)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.DeleteAsync(detailsPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await learner.PostAsJsonAsync(path, command)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ImportAsync(learner, path, workbook)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ImportAsync(learner, path, workbook, preview: true)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await learner.DeleteAsync(detailsPath)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await learner.GetAsync(detailsPath)).StatusCode);

        var imported = await ImportAsync(manager, path, workbook);
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        var outcome = await imported.Content.ReadFromJsonAsync<ImportOutcome>();
        Assert.True(outcome?.SuccessCount == 2, await imported.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync(detailsPath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync(detailsPath)).StatusCode);
    }

    [Fact]
    public async Task CurrentAccount_ReflectsRoleChangesForAnExistingToken()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        var account = await client.AuthenticateAsync();
        Assert.False((await client.GetFromJsonAsync<AccountCapabilities>("/api/auth/me"))!.CanManageCatalog);

        await factory.SetUserRoleAsync(account.UserId, UserRole.CatalogManager);
        Assert.True((await client.GetFromJsonAsync<AccountCapabilities>("/api/auth/me"))!.CanManageCatalog);

        await factory.SetUserRoleAsync(account.UserId, UserRole.Learner);
        Assert.False((await client.GetFromJsonAsync<AccountCapabilities>("/api/auth/me"))!.CanManageCatalog);
        var (path, command) = CatalogEntry(CardType.Vocab);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(path, command)).StatusCode);
    }

    [Fact]
    public async Task LearnerDelete_ShouldPreserveAnotherLearnersCardDuringConcurrentAdd()
    {
        using var manager = factory.CreateClient();
        using var learner = factory.CreateClient();
        using var otherLearner = factory.CreateClient();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        await learner.AuthenticateAsync();
        await otherLearner.AuthenticateAsync();
        var (path, command) = CatalogEntry(CardType.Vocab);
        var create = await manager.PostAsJsonAsync(path, command);
        create.EnsureSuccessStatusCode();
        var vocabId = await create.Content.ReadFromJsonAsync<Guid>();

        var attempts = await Task.WhenAll(
            learner.DeleteAsync($"{path}/{vocabId}"),
            otherLearner.PostAsJsonAsync("/api/reviewcards", new AddToReviewCommand(CardType.Vocab, vocabId)));
        Assert.Equal(HttpStatusCode.Forbidden, attempts[0].StatusCode);
        Assert.Equal(HttpStatusCode.OK, attempts[1].StatusCode);

        var due = await otherLearner.GetFromJsonAsync<List<DueCardDto>>("/api/reviewcards/due");
        Assert.Contains(due!, card => card.ReferenceId == vocabId);
        Assert.Equal(HttpStatusCode.OK, (await otherLearner.GetAsync($"{path}/{vocabId}")).StatusCode);
    }

    private static (string Path, object Command) CatalogEntry(CardType cardType) => cardType switch
    {
        CardType.Vocab => ("/api/vocabs", new CreateVocabCommand("歩く", "あるく", "đi bộ", null, null, JlptLevel.N5)),
        CardType.Kanji => ("/api/kanjis", new CreateKanjiCommand("山", "サン", "やま", "núi", 3, JlptLevel.N5)),
        CardType.Grammar => ("/api/grammarpoints", new CreateGrammarPointCommand("〜ながら", "vừa làm vừa...", null, null, JlptLevel.N5)),
        _ => throw new ArgumentOutOfRangeException(nameof(cardType))
    };

    private static async Task<HttpResponseMessage> ImportAsync(HttpClient client, string path, byte[] workbook, bool preview = false)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(workbook), "file", "catalog.xlsx");
        return await client.PostAsync(preview ? $"{path}/import/preview" : $"{path}/import", form);
    }

    public sealed record ImportOutcome(int SuccessCount);
}
