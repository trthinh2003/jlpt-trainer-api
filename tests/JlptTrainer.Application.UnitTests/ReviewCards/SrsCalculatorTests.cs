using FluentAssertions;
using JlptTrainer.Application.ReviewCards.Commands.SubmitReview;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Application.UnitTests.ReviewCards
{
    public class SrsCalculatorTests
    {
        private static readonly DateTimeOffset ReviewedAt = new(2026, 7, 22, 8, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Calculate_WhenGradeIsAgain_ShouldResetRepetitionsAndReviewTomorrow()
        {       
            var result = SrsCalculator.Calculate(  // Arrange: thẻ đã ôn được 3 lần đúng liên tiếp, giờ quên
                currentEaseFactor: 2.5,
                currentRepetitions: 3,
                currentIntervalDays: 15,
                grade: ReviewGrade.Again,
                reviewedAt: ReviewedAt);

            result.Repetitions.Should().Be(0);
            result.IntervalDays.Should().Be(1);
            result.NextReviewDate.Should().Be(ReviewedAt.AddDays(1));
            
            result.EaseFactor.Should().Be(2.5); // nhớ sai chỉ reset tiến độ, không phạt thêm
        }

        [Fact]
        public void Calculate_FirstCorrectReview_ShouldSetIntervalToOneDay()
        {
            var result = SrsCalculator.Calculate(
                currentEaseFactor: 2.5,
                currentRepetitions: 0,
                currentIntervalDays: 0,
                grade: ReviewGrade.Good,
                reviewedAt: ReviewedAt);

            result.Repetitions.Should().Be(1);
            result.IntervalDays.Should().Be(1);
            result.NextReviewDate.Should().Be(ReviewedAt.AddDays(1));
        }

        [Fact]
        public void Calculate_SecondCorrectReview_ShouldSetIntervalToSixDays()
        {
            var result = SrsCalculator.Calculate(
                currentEaseFactor: 2.5,
                currentRepetitions: 1,
                currentIntervalDays: 1,
                grade: ReviewGrade.Good,
                reviewedAt: ReviewedAt);

            result.Repetitions.Should().Be(2);
            result.IntervalDays.Should().Be(6);
        }

        [Fact]
        public void Calculate_ThirdCorrectReview_ShouldMultiplyIntervalByEaseFactor()
        {
            // lần 3 trở đi: interval mới = interval cũ * EaseFactor
            var result = SrsCalculator.Calculate(
                currentEaseFactor: 2.5,
                currentRepetitions: 2,
                currentIntervalDays: 6,
                grade: ReviewGrade.Good,
                reviewedAt: ReviewedAt);

            result.Repetitions.Should().Be(3);
            result.IntervalDays.Should().Be(15); // round(6 * 2.5) = 15
        }

        [Fact]
        public void Calculate_WhenGradeIsEasy_ShouldIncreaseEaseFactor()
        {
            var result = SrsCalculator.Calculate(
                currentEaseFactor: 2.5,
                currentRepetitions: 2,
                currentIntervalDays: 6,
                grade: ReviewGrade.Easy,
                reviewedAt: ReviewedAt);

            result.EaseFactor.Should().BeGreaterThan(2.5);
        }

        [Fact]
        public void Calculate_WhenGradeIsHard_ShouldDecreaseEaseFactor()
        {
            var result = SrsCalculator.Calculate(
                currentEaseFactor: 2.5,
                currentRepetitions: 2,
                currentIntervalDays: 6,
                grade: ReviewGrade.Hard,
                reviewedAt: ReviewedAt);

            result.EaseFactor.Should().BeLessThan(2.5);
        }

        [Fact]
        public void Calculate_EaseFactor_ShouldNeverGoBelowMinimum()
        {
            double easeFactor = 1.35;

            for (int i = 0; i < 10; i++)
            {
                var result = SrsCalculator.Calculate(
                    currentEaseFactor: easeFactor,
                    currentRepetitions: 2,
                    currentIntervalDays: 6,
                    grade: ReviewGrade.Hard,
                    reviewedAt: ReviewedAt);

                easeFactor = result.EaseFactor;
            }

            easeFactor.Should().BeGreaterThanOrEqualTo(1.3);
        }

        [Fact]
        public void InitialState_ShouldReturnDefaultEaseFactorAndZeroRepetitions()
        {
            var result = SrsCalculator.InitialState(ReviewedAt);

            result.EaseFactor.Should().Be(2.5);
            result.Repetitions.Should().Be(0);
            result.IntervalDays.Should().Be(0);
            result.NextReviewDate.Should().Be(ReviewedAt);
        }
    }
}
