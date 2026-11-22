using FluentAssertions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Vocabs;
using JlptTrainer.Application.Vocabs.Commands.CreateVocab;
using JlptTrainer.Application.Vocabs.ImportVocabFromExcel;
using JlptTrainer.Domain.Entities;
using NSubstitute;

namespace JlptTrainer.Application.UnitTests.Vocabs
{
    public class ImportVocabFromExcelCommandHandlerTests
    {
        private readonly IExcelReader _excelReader = Substitute.For<IExcelReader>();
        private readonly IVocabCatalogWriter _writer = Substitute.For<IVocabCatalogWriter>();

        private ImportVocabFromExcelCommandHandler CreateHandler() =>
            new(_writer, Substitute.For<IApplicationDbContext>(), _excelReader, new CreateVocabCommandValidator());

        [Fact]
        public async Task Handle_WithValidRows_ShouldImportAllVocabs()
        {
            var rows = new List<ExcelSheetRow>
            {
                Row(2, ("Word", "食べる"), ("Reading", "たべる"), ("Meaning", "ăn"), ("Level", "N5")),
                Row(3, ("Word", "飲む"), ("Reading", "のむ"), ("Meaning", "uống"), ("Level", "5"))
            };
            _excelReader.ReadSheet(Arg.Any<Stream>()).Returns(rows);
            _writer.InsertMissingAsync(Arg.Any<IReadOnlyList<Vocab>>(), CancellationToken.None)
                .Returns(call => call.Arg<IReadOnlyList<Vocab>>()!.Count);

            var handler = CreateHandler();
            var result = await handler.Handle(new ImportVocabFromExcelCommand([1, 2, 3]), CancellationToken.None);

            result.AcceptedCount.Should().Be(2);
            result.Errors.Should().BeEmpty();
            await _writer.Received(1).InsertMissingAsync(
                Arg.Is<IReadOnlyList<Vocab>>(vocabs => vocabs != null && vocabs.Count == 2 &&
                    vocabs[0].Word == "食べる" && vocabs[1].Word == "飲む"),
                CancellationToken.None);
        }

        [Fact]
        public async Task Handle_WhenRowMissingRequiredField_ShouldReportError()
        {
            var rows = new List<ExcelSheetRow>
            {
                Row(4, ("Word", "食べる"), ("Reading", ""), ("Meaning", "ăn"), ("Level", "N5"))
            };
            _excelReader.ReadSheet(Arg.Any<Stream>()).Returns(rows);

            var handler = CreateHandler();
            var result = await handler.Handle(new ImportVocabFromExcelCommand([1]), CancellationToken.None);

            result.AcceptedCount.Should().Be(0);
            result.Errors.Should().ContainSingle();
            result.Errors[0].RowNumber.Should().Be(4);
        }

        [Fact]
        public async Task Handle_WhenVocabAlreadyExistsInDb_ShouldSkipAsDuplicate()
        {
            var rows = new List<ExcelSheetRow>
            {
                Row(2, ("Word", "食べる"), ("Reading", "たべる"), ("Meaning", "ăn (mới, trùng)"), ("Level", "N5"))
            };
            _excelReader.ReadSheet(Arg.Any<Stream>()).Returns(rows);
            _writer.InsertMissingAsync(Arg.Any<IReadOnlyList<Vocab>>(), CancellationToken.None).Returns(0);

            var handler = CreateHandler();
            var result = await handler.Handle(new ImportVocabFromExcelCommand([1]), CancellationToken.None);

            result.AcceptedCount.Should().Be(0);
            result.SkippedDuplicateCount.Should().Be(1);
            await _writer.Received(1).InsertMissingAsync(
                Arg.Is<IReadOnlyList<Vocab>>(vocabs => vocabs != null && vocabs.Count == 1 && vocabs[0].Word == "食べる"), CancellationToken.None);
        }

        [Fact]
        public async Task Handle_WhenDuplicateWithinSameFile_ShouldOnlyImportFirstOccurrence()
        {
            var rows = new List<ExcelSheetRow>
            {
                Row(2, ("Word", "話す"), ("Reading", "はなす"), ("Meaning", "nói"), ("Level", "N5")),
                Row(3, ("Word", "話す"), ("Reading", "はなす"), ("Meaning", "nói (dòng trùng)"), ("Level", "N5"))
            };
            _excelReader.ReadSheet(Arg.Any<Stream>()).Returns(rows);
            _writer.InsertMissingAsync(Arg.Any<IReadOnlyList<Vocab>>(), CancellationToken.None)
                .Returns(call => call.Arg<IReadOnlyList<Vocab>>()!.Count);

            var handler = CreateHandler();
            var result = await handler.Handle(new ImportVocabFromExcelCommand([1]), CancellationToken.None);

            result.AcceptedCount.Should().Be(1);
            result.SkippedDuplicateCount.Should().Be(1);
            await _writer.Received(1).InsertMissingAsync(
                Arg.Is<IReadOnlyList<Vocab>>(vocabs => vocabs != null && vocabs.Count == 1 && vocabs[0].Meaning == "nói"), CancellationToken.None);
        }

        private static ExcelSheetRow Row(int number, params (string Key, string? Value)[] cells) =>
            new(number, cells.ToDictionary(c => c.Key, c => c.Value));

    }
}
