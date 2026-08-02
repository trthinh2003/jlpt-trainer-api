using FluentAssertions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.MockTests.Commands.SubmitMockTest;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using NSubstitute;

namespace JlptTrainer.Application.UnitTests.MockTests
{
    public class SubmitMockTestCommandHandlerTests : IDisposable
    {
        private readonly ApplicationDbContextFake _dbContext = new();
        private readonly ICurrentUserService _currentUser;
        private readonly Guid _userId = Guid.NewGuid();

        public SubmitMockTestCommandHandlerTests()
        {
            _currentUser = Substitute.For<ICurrentUserService>();
            _currentUser.UserId.Returns(_userId);
        }

        [Fact]
        public async Task Handle_GradesAgainstDbMeaning_NotAgainstWhatClientClaims()
        {
            var vocab = new Vocab { Word = "食べる", Reading = "たべる", Meaning = "ăn" };
            _dbContext.Vocabs.Add(vocab);
            await _dbContext.SaveChangesAsync();

            var command = new SubmitMockTestCommand( // gửi đáp án khớp với nghĩa thật -> tính đúng
                JlptLevel.N5,
                [new MockTestAnswer(vocab.Id, "ăn")],
                TimeSpan.FromMinutes(2));

            var handler = new SubmitMockTestCommandHandler(_dbContext, _currentUser, TimeProvider.System);
            var result = await handler.Handle(command, CancellationToken.None);

            result.CorrectAnswers.Should().Be(1);
            result.ScorePercentage.Should().Be(100);
        }

        [Fact]
        public async Task Handle_WhenSelectedMeaningDoesNotMatchDb_ShouldCountAsIncorrect()
        {
            var vocab = new Vocab { Word = "飲む", Reading = "のむ", Meaning = "uống" };
            _dbContext.Vocabs.Add(vocab);
            await _dbContext.SaveChangesAsync();

            var command = new SubmitMockTestCommand(
                JlptLevel.N5,
                [new MockTestAnswer(vocab.Id, "ăn")], // sai - vocab thật có nghĩa "uống"
                TimeSpan.FromMinutes(2));

            var handler = new SubmitMockTestCommandHandler(_dbContext, _currentUser, TimeProvider.System);
            var result = await handler.Handle(command, CancellationToken.None);

            result.CorrectAnswers.Should().Be(0);
            result.ScorePercentage.Should().Be(0);
        }

        [Fact]
        public async Task Handle_ComparisonIsCaseInsensitiveAndTrimmed()
        {
            var vocab = new Vocab { Word = "読む", Reading = "よむ", Meaning = "đọc" };
            _dbContext.Vocabs.Add(vocab);
            await _dbContext.SaveChangesAsync();

            var command = new SubmitMockTestCommand(
                JlptLevel.N5,
                [new MockTestAnswer(vocab.Id, "  ĐỌC  ")], // khác hoa/thường + thừa khoảng trắng
                TimeSpan.FromMinutes(1));

            var handler = new SubmitMockTestCommandHandler(_dbContext, _currentUser, TimeProvider.System);
            var result = await handler.Handle(command, CancellationToken.None);

            result.CorrectAnswers.Should().Be(1);
        }

        [Fact]
        public async Task Handle_ShouldPersistMockTestSummaryToDb()
        {
            var vocab = new Vocab { Word = "書く", Reading = "かく", Meaning = "viết" };
            _dbContext.Vocabs.Add(vocab);
            await _dbContext.SaveChangesAsync();

            var command = new SubmitMockTestCommand(
                JlptLevel.N5,
                [new MockTestAnswer(vocab.Id, "viết")],
                TimeSpan.FromMinutes(3));

            var handler = new SubmitMockTestCommandHandler(_dbContext, _currentUser, TimeProvider.System);
            await handler.Handle(command, CancellationToken.None);

            _dbContext.MockTests.Count().Should().Be(1);
            _dbContext.MockTests.First().UserId.Should().Be(_userId);
        }

        public void Dispose() => _dbContext.Dispose();
    }
}
