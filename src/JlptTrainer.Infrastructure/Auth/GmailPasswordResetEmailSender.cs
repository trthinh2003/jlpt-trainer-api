using System.Net;
using System.Net.Mail;
using JlptTrainer.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace JlptTrainer.Infrastructure.Auth;

internal sealed class GmailPasswordResetEmailSender(IOptions<PasswordResetSettings> options) : IPasswordResetEmailSender
{
    public async Task SendAsync(string email, string token, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var link = new Uri(new Uri(settings.FrontendOrigin), $"/reset-password#token={token}");
        using var message = new MailMessage(settings.GmailAddress, email)
        {
            Subject = "Đặt lại mật khẩu JLPT Trainer",
            Body = $"""
                Mở liên kết sau để đặt lại mật khẩu. Liên kết có hiệu lực trong 15 phút và dùng một lần.

                {link.AbsoluteUri}

                Nếu bạn không yêu cầu đổi mật khẩu, bạn có thể bỏ qua email này.
                """
        };
        using var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(settings.GmailAddress, settings.GmailAppPassword.Replace(" ", ""))
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        await smtp.SendMailAsync(message, timeout.Token);
    }
}
