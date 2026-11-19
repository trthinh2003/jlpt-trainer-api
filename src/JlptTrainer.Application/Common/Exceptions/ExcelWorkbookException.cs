namespace JlptTrainer.Application.Common.Exceptions;

public sealed class ExcelWorkbookException(string message, Exception? innerException = null)
    : Exception(message, innerException);
