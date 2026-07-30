using FluentAssertions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.Vocabs.ImportVocabFromExcel;
using JlptTrainer.Domain.Entities;
using NSubstitute;

namespace JlptTrainer.Application.UnitTests.Vocabs
{
    public class ImportVocabFromExcelCommandHandlerTests : IDisposable
    {
        private readonly ApplicationDbContextFake _dbContext = new();
        private readonly IExcelReader _excelReader = Substitute.For<IExcelReader>();

        [Fact]
        public async Task Handle_WithValidRows_ShouldImportAllVocabs()
        {
            var rows = new List<IReadOnlyDictionary<string, string?>>
            {
                Row(("Word", "食べる"), ("Reading", "たべる"), ("Meaning", "ăn"), ("Level", "N5")),
                Row(("Word", "飲む"), ("Reading", "のむ"), ("Meaning", "uống"), ("Level", "5"))
            };
            _excelReader.ReadSheet(Arg.Any<Stream>()).Returns(rows);

            var handler = new ImportVocabFromExcelCommandHandler(_dbContext, _excelReader);
            var result = await handler.Handle(new ImportVocabFromExcelCommand([1, 2, 3]), CancellationToken.None);

            result.SuccessCount.Should().Be(2);
            result.Errors.Should().BeEmpty();
            _dbContext.Vocabs.Count().Should().Be(2);
        }

        [Fact]
        public async Task Handle_WhenRowMissingRequiredField_ShouldReportError()
        {
            var rows = new List<IReadOnlyDictionary<string, string?>>
            {
                Row(("Word", "食べる"), ("Reading", ""), ("Meaning", "ăn"), ("Level", "N5"))
            };
            _excelReader.ReadSheet(Arg.Any<Stream>()).Returns(rows);

            var handler = new ImportVocabFromExcelCommandHandler(_dbContext, _excelReader);
            var result = await handler.Handle(new ImportVocabFromExcelCommand([1]), CancellationToken.None);

            result.SuccessCount.Should().Be(0);
            result.Errors.Should().ContainSingle();
            result.Errors[0].RowNumber.Should().Be(2); // dòng đầu tiên sau header = dòng 2
        }

        [Fact]
        public async Task Handle_WhenVocabAlreadyExistsInDb_ShouldSkipAsDuplicate()
        {
            _dbContext.Vocabs.Add(new Vocab { Word = "食べる", Reading = "たべる", Meaning = "ăn (cũ)" });
            await _dbContext.SaveChangesAsync();

            var rows = new List<IReadOnlyDictionary<string, string?>>
            {
                Row(("Word", "食べる"), ("Reading", "たべる"), ("Meaning", "ăn (mới, trùng)"), ("Level", "N5"))
            };
            _excelReader.ReadSheet(Arg.Any<Stream>()).Returns(rows);

            var handler = new ImportVocabFromExcelCommandHandler(_dbContext, _excelReader);
            var result = await handler.Handle(new ImportVocabFromExcelCommand([1]), CancellationToken.None);

            result.SuccessCount.Should().Be(0);
            result.SkippedDuplicateCount.Should().Be(1);
            _dbContext.Vocabs.Count().Should().Be(1);
        }

        [Fact]
        public async Task Handle_WhenDuplicateWithinSameFile_ShouldOnlyImportFirstOccurrence()
        {
            var rows = new List<IReadOnlyDictionary<string, string?>>
            {
                Row(("Word", "話す"), ("Reading", "はなす"), ("Meaning", "nói"), ("Level", "N5")),
                Row(("Word", "話す"), ("Reading", "はなす"), ("Meaning", "nói (dòng trùng)"), ("Level", "N5"))
            };
            _excelReader.ReadSheet(Arg.Any<Stream>()).Returns(rows);

            var handler = new ImportVocabFromExcelCommandHandler(_dbContext, _excelReader);
            var result = await handler.Handle(new ImportVocabFromExcelCommand([1]), CancellationToken.None);

            result.SuccessCount.Should().Be(1);
            result.SkippedDuplicateCount.Should().Be(1);
        }

        private static IReadOnlyDictionary<string, string?> Row(params (string Key, string? Value)[] cells) => cells.ToDictionary(c => c.Key, c => c.Value);

        public void Dispose() => _dbContext.Dispose();
    }
}
