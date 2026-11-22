using JlptTrainer.Application.Common.ExcelImport;
using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Commands.ImportGrammarPointFromExcel;

public sealed record ImportGrammarPointFromExcelCommand(byte[] FileContent, bool PreviewOnly = false) : IRequest<CatalogImportResult>;
