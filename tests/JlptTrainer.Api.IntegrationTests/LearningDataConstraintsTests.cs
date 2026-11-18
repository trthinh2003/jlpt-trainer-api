using JlptTrainer.Application.ReviewCards.Commands.SubmitReview;
using JlptTrainer.Domain.Entities;
using JlptTrainer.Domain.Enums;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class LearningDataConstraintsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task SaveChanges_WhenReviewCardOwnerDoesNotExist_ShouldRejectCard()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var vocab = new Vocab { Word = "走る", Reading = "はしる", Meaning = "chạy" };
        db.Vocabs.Add(vocab);
        await db.SaveChangesAsync();

        var initialState = SrsCalculator.InitialState(DateTimeOffset.UtcNow);
        db.ReviewCards.Add(new ReviewCard(Guid.NewGuid(), CardType.Vocab, vocab.Id, initialState));

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var cause = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, cause.SqlState);
        Assert.Equal("fk_review_cards_users_user_id", cause.ConstraintName);
    }

    [Fact]
    public async Task SaveChanges_WhenKanjiHasNoStrokes_ShouldRejectKanji()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Kanjis.Add(new Kanji { Character = "山", Meaning = "núi", StrokeCount = 0 });

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var cause = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, cause.SqlState);
        Assert.Equal("ck_kanjis_positive_stroke_count", cause.ConstraintName);
    }
}
