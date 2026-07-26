namespace JlptTrainer.Application.Common.Exceptions
{
    public sealed class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException() : base("Email hoặc mật khẩu không đúng.") { }
    }
}
