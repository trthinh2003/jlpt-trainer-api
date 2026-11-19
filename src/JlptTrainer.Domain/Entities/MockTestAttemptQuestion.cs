namespace JlptTrainer.Domain.Entities;

public sealed class MockTestAttemptQuestion
{
    private MockTestAttemptQuestion() { }

    public MockTestAttemptQuestion(int position, Guid sourceVocabId, string word, string reading, string[] choices, int correctChoiceIndex)
    {
        Position = position;
        SourceVocabId = sourceVocabId;
        Word = word;
        Reading = reading;
        Choices = choices;
        CorrectChoiceIndex = correctChoiceIndex;
    }

    public Guid AttemptId { get; private set; }
    public int Position { get; private set; }
    public Guid? SourceVocabId { get; private set; }
    public string Word { get; private set; } = null!;
    public string Reading { get; private set; } = null!;
    public string[] Choices { get; private set; } = [];
    public int CorrectChoiceIndex { get; private set; }
}
