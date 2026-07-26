using JlptTrainer.Domain.Enums;
using JlptTrainer.Domain.ValueObjects;

namespace JlptTrainer.Application.ReviewCards.Commands.SubmitReview
{
    public static class SrsCalculator
    {
        private const double MinEaseFactor = 1.3;
        private const double DefaultEaseFactor = 2.5;

        public static SrsResult Calculate(
            double currentEaseFactor,
            int currentRepetitions,
            int currentIntervalDays,
            ReviewGrade grade,
            DateTimeOffset reviewedAt
        )
        {
            /* quy đổi ReviewGrade (0-3) sang thang điểm chất lượng gốc SM-2 (0-5).
               Again=2 thay vì 0 vì trong SM-2 gốc, 0-2 bị coi là quên và đều reset
               repetitions về 0 -> không cần phân biệt sâu hơn ở mức UI hiện tại. */
            var quality = grade switch
            {
                ReviewGrade.Again => 2,
                ReviewGrade.Hard => 3,
                ReviewGrade.Good => 4,
                ReviewGrade.Easy => 5,
                _ => throw new ArgumentOutOfRangeException(nameof(grade), grade, "Grade không hợp lệ")
            };
            
            if (quality < 3) // nhớ sai (quality < 3) -> reset về đầu, ôn lại ngay hôm sau
            {
                return new SrsResult(
                    EaseFactor: currentEaseFactor, // giữ nguyên EaseFactor, không phạt thêm ở bước reset
                    IntervalDays: 1,
                    Repetitions: 0,
                    NextReviewDate: reviewedAt.AddDays(1));
            }
         
            var newRepetitions = currentRepetitions + 1; // nhớ đúng -> tăng interval theo cấp số nhân dựa trên EaseFactor

            var newIntervalDays = newRepetitions switch
            {
                1 => 1,
                2 => 6,
                _ => (int)Math.Round(currentIntervalDays * currentEaseFactor)
            };

            // công thức gốc SM-2 điều chỉnh EaseFactor theo chất lượng trả lời
            var newEaseFactor = currentEaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));

            newEaseFactor = Math.Max(newEaseFactor, MinEaseFactor);

            return new SrsResult(
                EaseFactor: newEaseFactor,
                IntervalDays: newIntervalDays,
                Repetitions: newRepetitions,
                NextReviewDate: reviewedAt.AddDays(newIntervalDays)
            );
        }

        /// state mặc định cho 1 thẻ hoàn toàn mới, chưa ôn lần nào
        public static SrsResult InitialState(DateTimeOffset now) => new(DefaultEaseFactor, IntervalDays: 0, Repetitions: 0, NextReviewDate: now);
    }
}
