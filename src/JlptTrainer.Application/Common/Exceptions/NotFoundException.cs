namespace JlptTrainer.Application.Common.Exceptions
{
    public sealed class NotFoundException : Exception
    {
        public NotFoundException(string entityName, object key) : base($"{entityName} với id \"{key}\" không tồn tại.") { }
    }
}
