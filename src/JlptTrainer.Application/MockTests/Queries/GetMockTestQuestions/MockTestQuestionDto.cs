namespace JlptTrainer.Application.MockTests.Queries.GetMockTestQuestions
{
    public sealed record MockTestQuestionDto(
        Guid VocabId,
        string Word,
        string Reading,
        List<string> Choices);
}
