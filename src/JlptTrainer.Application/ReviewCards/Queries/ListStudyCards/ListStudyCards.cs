using FluentValidation;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.ReviewCards.Queries.ListStudyCards;

public sealed record ListStudyCards(
    int PageNumber = 1, int PageSize = 20, CardType? CardType = null, JlptLevel? Level = null,
    bool? Due = null, string? Search = null) : IRequest<StudyCardPage>;

public sealed record StudyCardItem(
    Guid Id, CardType CardType, JlptLevel Level, string Prompt, string Meaning,
    DateTimeOffset NextReviewDate, DateTimeOffset? LastReviewedAt, bool IsDue);

public sealed record StudyCardPage(List<StudyCardItem> Items, int TotalCount, int PageNumber, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class ListStudyCardsValidator : AbstractValidator<ListStudyCards>
{
    public ListStudyCardsValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.CardType).IsInEnum();
        RuleFor(query => query.Level).IsInEnum();
        RuleFor(query => query.Search).MaximumLength(100);
    }
}

public sealed class ListStudyCardsHandler(IApplicationDbContext db, ICurrentUserService currentUser, TimeProvider timeProvider)
    : IRequestHandler<ListStudyCards, StudyCardPage>
{
    public async Task<StudyCardPage> Handle(ListStudyCards request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var now = timeProvider.GetUtcNow();
        var cards = db.ReviewCards.Where(card => card.UserId == userId);
        if (request.CardType is { } type) cards = cards.Where(card => card.CardType == type);
        if (request.Due is { } due) cards = cards.Where(card => (card.NextReviewDate <= now) == due);

        var vocab = from card in cards.Where(card => card.CardType == CardType.Vocab)
                    join item in db.Vocabs on card.ReferenceId equals item.Id
                    select new { card.Id, card.CardType, item.Level, Prompt = item.Word, item.Meaning,
                        Reading = item.Reading, card.NextReviewDate, card.LastReviewedAt };
        var kanji = from card in cards.Where(card => card.CardType == CardType.Kanji)
                    join item in db.Kanjis on card.ReferenceId equals item.Id
                    select new { card.Id, card.CardType, item.Level, Prompt = item.Character, item.Meaning,
                        Reading = item.OnYomi + " " + item.KunYomi, card.NextReviewDate, card.LastReviewedAt };
        var grammar = from card in cards.Where(card => card.CardType == CardType.Grammar)
                      join item in db.GrammarPoints on card.ReferenceId equals item.Id
                      select new { card.Id, card.CardType, item.Level, Prompt = item.Pattern, item.Meaning,
                          Reading = "", card.NextReviewDate, card.LastReviewedAt };
        var query = vocab.Concat(kanji).Concat(grammar);
        if (request.Level is { } level) query = query.Where(card => card.Level == level);

        var search = request.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(card => card.Prompt.ToLower().Contains(search)
                || card.Reading.ToLower().Contains(search) || card.Meaning.ToLower().Contains(search));
        }

        var count = await query.CountAsync(ct);
        var offset = (request.PageNumber - 1L) * request.PageSize;
        List<StudyCardItem> items = offset >= count ? [] : await query
            .OrderBy(card => card.NextReviewDate).ThenBy(card => card.Id)
            .Skip((int)offset).Take(request.PageSize)
            .Select(card => new StudyCardItem(card.Id, card.CardType, card.Level, card.Prompt, card.Meaning,
                card.NextReviewDate, card.LastReviewedAt, card.NextReviewDate <= now))
            .ToListAsync(ct);
        return new StudyCardPage(items, count, request.PageNumber, request.PageSize);
    }
}
