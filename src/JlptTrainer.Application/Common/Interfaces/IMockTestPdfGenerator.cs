namespace JlptTrainer.Application.Common.Interfaces
{
    public sealed record MockTestPdfData(
        string UserDisplayName,
        string Level,
        int TotalQuestions,
        int CorrectAnswers,
        double ScorePercentage,
        TimeSpan Duration,
        DateTimeOffset TakenAt);

    public interface IMockTestPdfGenerator
    {
        byte[] Generate(MockTestPdfData data);
    }
}
