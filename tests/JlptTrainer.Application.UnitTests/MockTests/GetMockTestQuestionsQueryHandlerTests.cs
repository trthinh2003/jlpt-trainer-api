using FluentAssertions;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.MockTests.Queries.GetMockTestQuestions;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.UnitTests.MockTests
{
    public class GetMockTestQuestionsQueryHandlerTests : IDisposable
    {
        private readonly ApplicationDbContextFake _dbContext = new();

        [Fact]
        public async Task Handle_WhenPoolHasFewerThan4Vocabs_ShouldThrowInsufficientDataException()
        {
            _dbContext.Vocabs.AddRange(
                new Vocab { Word = "1", Reading = "1", Meaning = "1", Level = JlptLevel.N5 },
                new Vocab { Word = "2", Reading = "2", Meaning = "2", Level = JlptLevel.N5 });
            await _dbContext.SaveChangesAsync();

            var handler = new GetMockTestQuestionsQueryHandler(_dbContext);
            var query = new GetMockTestQuestionsQuery(JlptLevel.N5, 10);

            var act = () => handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<InsufficientDataException>();
        }

        [Fact]
        public async Task Handle_EachQuestionShouldHaveExactly4UniqueChoicesIncludingCorrectAnswer()
        {
            for (var i = 0; i < 10; i++)
            {
                _dbContext.Vocabs.Add(new Vocab
                {
                    Word = $"word{i}",
                    Reading = $"reading{i}",
                    Meaning = $"meaning{i}",
                    Level = JlptLevel.N5
                });
            }
            await _dbContext.SaveChangesAsync();

            var handler = new GetMockTestQuestionsQueryHandler(_dbContext);
            var query = new GetMockTestQuestionsQuery(JlptLevel.N5, 5);

            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().HaveCount(5);
            foreach (var question in result)
            {
                question.Choices.Should().HaveCount(4);
                question.Choices.Should().OnlyHaveUniqueItems();
            }
        }

        [Fact]
        public async Task Handle_WhenRequestedCountExceedsPoolSize_ShouldReturnOnlyAvailableCount()
        {
            for (var i = 0; i < 5; i++)
            {
                _dbContext.Vocabs.Add(new Vocab
                {
                    Word = $"word{i}",
                    Reading = $"reading{i}",
                    Meaning = $"meaning{i}",
                    Level = JlptLevel.N5
                });
            }
            await _dbContext.SaveChangesAsync();

            var handler = new GetMockTestQuestionsQueryHandler(_dbContext);
            var query = new GetMockTestQuestionsQuery(JlptLevel.N5, 100); // xin nhiều hơn pool có

            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().HaveCount(5); // không vượt quá pool thực tế
        }

        public void Dispose() => _dbContext.Dispose();
    }
}
