using JlptTrainer.Application.Common.ExcelImport;
using MediatR;

namespace JlptTrainer.Application.Kanjis.Commands.ImportKanjiFromExcel;

public sealed record ImportKanjiFromExcelCommand(byte[] FileContent, bool PreviewOnly = false) : IRequest<CatalogImportResult>;
