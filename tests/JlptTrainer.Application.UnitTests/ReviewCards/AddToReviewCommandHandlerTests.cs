using FluentAssertions;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace JlptTrainer.Application.UnitTests.ReviewCards
{
    public class AddToReviewCommandHandlerTests : IDisposable
    {
        private readonly ApplicationDbContextFake _dbContext;
        private readonly ICurrentUserService _currentUser;
        private readonly TimeProvider _timeProvider;
        private readonly Guid _userId = Guid.NewGuid();

        public AddToReviewCommandHandlerTests()
        {
            _dbContext = new ApplicationDbContextFake();

            _currentUser = Substitute.For<ICurrentUserService>();
            _currentUser.UserId.Returns(_userId);

            _timeProvider = TimeProvider.System;
        }

        [Fact]
        public async Task Handle_WhenVocabExists_ShouldCreateReviewCardWithInitialState()
        {
            var vocab = new Vocab { Word = "食べる", Reading = "たべる", Meaning = "ăn" };
            _dbContext.Vocabs.Add(vocab);
            await _dbContext.SaveChangesAsync();

            var handler = new AddToReviewCommandHandler(_dbContext, _currentUser, _timeProvider);
            var command = new AddToReviewCommand(CardType.Vocab, vocab.Id);

            var cardId = await handler.Handle(command, CancellationToken.None);

            var createdCard = await _dbContext.ReviewCards.FirstAsync(c => c.Id == cardId);
            createdCard.UserId.Should().Be(_userId);
            createdCard.CardType.Should().Be(CardType.Vocab);
            createdCard.ReferenceId.Should().Be(vocab.Id);
            createdCard.EaseFactor.Should().Be(2.5);
            createdCard.Repetitions.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WhenVocabDoesNotExist_ShouldThrowNotFoundException()
        {
            var handler = new AddToReviewCommandHandler(_dbContext, _currentUser, _timeProvider);
            var command = new AddToReviewCommand(CardType.Vocab, Guid.NewGuid());

            var act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_WhenAlreadyAddedByUser_ShouldThrowConflictException()
        {
            var vocab = new Vocab { Word = "読む", Reading = "よむ", Meaning = "đọc" };
            _dbContext.Vocabs.Add(vocab);
            _dbContext.ReviewCards.Add(new ReviewCard
            {
                UserId = _userId,
                CardType = CardType.Vocab,
                ReferenceId = vocab.Id
            });
            await _dbContext.SaveChangesAsync();

            var handler = new AddToReviewCommandHandler(_dbContext, _currentUser, _timeProvider);
            var command = new AddToReviewCommand(CardType.Vocab, vocab.Id);

            var act = () => handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        public void Dispose() => _dbContext.Dispose();
    }
}
