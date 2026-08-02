namespace JlptTrainer.Application.MockTests.Commands.SubmitMockTest
{
    public sealed record SubmitMockTestResult(
        Guid MockTestId,
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage);
}
