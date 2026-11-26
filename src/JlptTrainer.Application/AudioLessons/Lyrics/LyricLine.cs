namespace JlptTrainer.Application.AudioLessons.Lyrics;

public sealed record LyricLine(int StartMs, string Text, LyricWordLink[]? Links = null);

public sealed record LyricWordLink(int Start, int Length, AudioLessonVocab Word);
