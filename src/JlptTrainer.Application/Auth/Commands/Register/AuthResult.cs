namespace JlptTrainer.Application.Auth.Commands.Register
{
    public sealed record AuthResult(
        Guid UserId,
        string Email,
        string DisplayName,
        string Token);

}
