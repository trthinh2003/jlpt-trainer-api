using JlptTrainer.Application.Common.ExcelImport;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JlptTrainer.Api.ExcelImport;

internal static class CatalogExcelUpload
{
    public const int RequestSizeLimit = 5 * 1024 * 1024 + 64 * 1024;

    public static async Task<ActionResult<TResponse>> HandleAsync<TResponse>(
        IFormFile file,
        ISender sender,
        Func<byte[], IRequest<CatalogImportResult>> createRequest,
        Func<CatalogImportResult, TResponse> createResponse,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return new BadRequestObjectResult(new { error = "File không được để trống." });
        }

        using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        var result = await sender.Send(createRequest(stream.ToArray()), cancellationToken);
        return new OkObjectResult(createResponse(result));
    }
}

public sealed record CatalogImportResponse(
    int TotalRows, int SuccessCount, int SkippedDuplicateCount, IReadOnlyList<CatalogImportRowError> Errors)
{
    internal static CatalogImportResponse From(CatalogImportResult result) =>
        new(result.TotalRows, result.AcceptedCount, result.SkippedDuplicateCount, result.Errors);
}

public sealed record CatalogImportPreviewResponse(
    int TotalRows, int ReadyCount, int SkippedDuplicateCount, IReadOnlyList<CatalogImportRowError> Errors)
{
    internal static CatalogImportPreviewResponse From(CatalogImportResult result) =>
        new(result.TotalRows, result.AcceptedCount, result.SkippedDuplicateCount, result.Errors);
}
