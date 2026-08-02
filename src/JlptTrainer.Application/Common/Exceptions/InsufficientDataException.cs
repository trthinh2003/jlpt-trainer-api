namespace JlptTrainer.Application.Common.Exceptions
{
    public sealed class InsufficientDataException : Exception
    {
        public InsufficientDataException(string message) : base(message) { }
    }
}
