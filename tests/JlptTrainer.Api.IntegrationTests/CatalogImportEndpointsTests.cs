using System.Net;
using System.Net.Http.Json;
using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Application.Vocabs.Queries.GetVocabList;
using JlptTrainer.Domain.Enums;
using OfficeOpenXml;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class CatalogImportEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Theory]
    [InlineData("vocabs", 3)]
    [InlineData("kanjis", 4)]
    [InlineData("grammarpoints", 2)]
    public async Task Import_ValidatesRowsAndReportsPhysicalExcelRowNumbers(string catalog, int meaningColumn)
    {
        using var manager = factory.CreateClient();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        var path = $"/api/{catalog}/import";
        var template = await manager.GetByteArrayAsync($"{path}/template");
        using var package = new ExcelPackage(new MemoryStream(template));
        var sheet = package.Workbook.Worksheets[0];
        var lastColumn = sheet.Dimension.End.Column;
        sheet.Cells[2, 1, 2, lastColumn].Copy(sheet.Cells[7, 1, 7, lastColumn]);
        sheet.Cells[7, meaningColumn].Value = new string('x', 501);
        sheet.Cells[2, 1, 2, lastColumn].Copy(sheet.Cells[9, 1, 9, lastColumn]);
        var workbook = package.GetAsByteArray();

        using var response = await UploadAsync(manager, path, workbook);
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ImportSummary>();
        Assert.NotNull(summary);
        Assert.Equal(4, summary.TotalRows);
        Assert.Equal(2, summary.SuccessCount);
        Assert.Equal(1, summary.SkippedDuplicateCount);
        Assert.Equal(7, Assert.Single(summary.Errors).RowNumber);

        using var replay = await UploadAsync(manager, path, workbook);
        replay.EnsureSuccessStatusCode();
        var replaySummary = await replay.Content.ReadFromJsonAsync<ImportSummary>();
        Assert.NotNull(replaySummary);
        Assert.Equal(0, replaySummary.SuccessCount);
        Assert.Equal(3, replaySummary.SkippedDuplicateCount);
        Assert.Equal(7, Assert.Single(replaySummary.Errors).RowNumber);
    }

    [Fact]
    public async Task Import_InvalidWorkbook_ReturnsReadableBadRequest()
    {
        using var manager = factory.CreateClient();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        using var response = await UploadAsync(manager, "/api/vocabs/import", "not an Excel workbook"u8.ToArray());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ImportError>();
        Assert.Contains(".xlsx", error!.Error);
    }

    [Fact]
    public async Task VocabCreateAndImports_CanRaceWithoutCreatingDuplicates()
    {
        using var manager = factory.CreateClient();
        await manager.AuthenticateAsCatalogManagerAsync(factory);
        const string catalog = "/api/vocabs";
        const string sharedWord = "同時単語";
        const string sharedReading = "どうじたんご";
        var template = await manager.GetByteArrayAsync($"{catalog}/import/template");
        using var package = new ExcelPackage(new MemoryStream(template));
        var sheet = package.Workbook.Worksheets[0];
        sheet.Cells[2, 1].Value = sharedWord;
        sheet.Cells[2, 2].Value = sharedReading;
        sheet.Cells[3, 1].Value = "同時別語";
        sheet.Cells[3, 2].Value = "どうじべつご";
        var workbook = package.GetAsByteArray();
        var direct = new CreateVocabCommand($" {sharedWord} ", $" {sharedReading} ", "từ thử", null, null, JlptLevel.N3);

        var outcomes = await Task.WhenAll(
            UploadAsync(manager, $"{catalog}/import", workbook),
            UploadAsync(manager, $"{catalog}/import", workbook),
            manager.PostAsJsonAsync(catalog, direct));

        var inserted = 0;
        foreach (var outcome in outcomes.Take(2))
        {
            using (outcome)
            {
                outcome.EnsureSuccessStatusCode();
                var summary = (await outcome.Content.ReadFromJsonAsync<ImportSummary>())!;
                Assert.Empty(summary.Errors);
                Assert.Equal(2, summary.SuccessCount + summary.SkippedDuplicateCount);
                inserted += summary.SuccessCount;
            }
        }

        using var create = outcomes[2];
        Assert.Contains(create.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
        Assert.Equal(2, inserted + (create.StatusCode == HttpStatusCode.Created ? 1 : 0));

        using var duplicate = await manager.PostAsJsonAsync(catalog, direct with { Level = JlptLevel.N1 });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var list = (await manager.GetFromJsonAsync<PagedVocabResult>($"{catalog}?search={Uri.EscapeDataString("同時")}"))!;
        Assert.Equal(2, list.TotalCount);
        Assert.Single(list.Items, vocab => vocab.Word == sharedWord && vocab.Reading == sharedReading);
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string path, byte[] bytes)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes), "file", "catalog.xlsx");
        return await client.PostAsync(path, form);
    }

    public sealed record ImportSummary(int TotalRows, int SuccessCount, int SkippedDuplicateCount, List<RowError> Errors);
    public sealed record RowError(int RowNumber, string Message);
    public sealed record ImportError(string Error);
}
