namespace JlptTrainer.Application.Common.Interfaces;

public interface IPasswordResetEmailSender
{
    Task SendAsync(string email, string token, CancellationToken cancellationToken);
}
