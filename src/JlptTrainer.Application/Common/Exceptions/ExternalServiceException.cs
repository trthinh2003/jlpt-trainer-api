namespace JlptTrainer.Application.Common.Exceptions
{
    public sealed class ExternalServiceException : Exception
    {
        public ExternalServiceException(string serviceName, string message) 
            : base($"Không thể kết nối tới {serviceName}: {message}") { }
    }
}
