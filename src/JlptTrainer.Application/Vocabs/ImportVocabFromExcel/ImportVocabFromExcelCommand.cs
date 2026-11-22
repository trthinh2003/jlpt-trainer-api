using JlptTrainer.Application.Common.ExcelImport;
using MediatR;

namespace JlptTrainer.Application.Vocabs.ImportVocabFromExcel;

public sealed record ImportVocabFromExcelCommand(byte[] FileContent, bool PreviewOnly = false) : IRequest<CatalogImportResult>;
