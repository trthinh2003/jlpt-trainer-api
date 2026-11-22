using FluentAssertions;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Application.ReviewCards;
using JlptTrainer.Application.ReviewCards.Commands.AddToReview;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using NSubstitute;

namespace JlptTrainer.Application.UnitTests.ReviewCards;

public class AddToReviewCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IReviewCardStore _store = Substitute.For<IReviewCardStore>();
    private readonly AddToReviewCommandHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public AddToReviewCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new AddToReviewCommandHandler(_currentUser, TimeProvider.System, _store);
    }

    [Fact]
    public async Task Handle_WhenVocabExists_ShouldCreateReviewCardWithInitialState()
    {
        var referenceId = Guid.NewGuid();
        var cards = new List<ReviewCard>();
        _store.AddMissingAsync(_userId, CardType.Vocab, Arg.Do<IReadOnlyList<ReviewCard>>(cards.AddRange), CancellationToken.None)
            .Returns(new ReviewCardAddition([referenceId], [], []));

        var cardId = await _handler.Handle(new AddToReviewCommand(CardType.Vocab, referenceId), CancellationToken.None);
        var createdCard = Assert.Single(cards);
        createdCard.Id.Should().Be(cardId);
        createdCard.UserId.Should().Be(_userId);
        createdCard.CardType.Should().Be(CardType.Vocab);
        createdCard.ReferenceId.Should().Be(referenceId);
        createdCard.EaseFactor.Should().Be(2.5);
        createdCard.Repetitions.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenVocabDoesNotExist_ShouldThrowNotFoundException()
    {
        var command = new AddToReviewCommand(CardType.Vocab, Guid.NewGuid());
        _store.AddMissingAsync(_userId, CardType.Vocab, Arg.Any<IReadOnlyList<ReviewCard>>(), CancellationToken.None)
            .Returns(new ReviewCardAddition([], [], [command.ReferenceId]));

        var act = () => _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyAddedByUser_ShouldThrowConflictException()
    {
        var command = new AddToReviewCommand(CardType.Grammar, Guid.NewGuid());
        _store.AddMissingAsync(_userId, CardType.Grammar, Arg.Any<IReadOnlyList<ReviewCard>>(), CancellationToken.None)
            .Returns(new ReviewCardAddition([], [command.ReferenceId], []));

        var act = () => _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }
}