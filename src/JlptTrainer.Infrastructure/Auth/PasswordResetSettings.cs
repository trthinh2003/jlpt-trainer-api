using System.Net.Mail;

namespace JlptTrainer.Infrastructure.Auth;

public sealed class PasswordResetSettings
{
    public const string SectionName = "PasswordReset";

    public bool Enabled { get; set; }
    public string FrontendOrigin { get; set; } = "http://localhost:5173";
    public string GmailAddress { get; set; } = string.Empty;
    public string GmailAppPassword { get; set; } = string.Empty;

    internal bool IsValid() => !Enabled || (
        Uri.TryCreate(FrontendOrigin, UriKind.Absolute, out var origin)
        && (origin.Scheme == Uri.UriSchemeHttps || (origin.Scheme == Uri.UriSchemeHttp && origin.IsLoopback))
        && origin.AbsolutePath == "/"
        && origin.Query.Length == 0 && origin.Fragment.Length == 0 && origin.UserInfo.Length == 0
        && MailAddress.TryCreate(GmailAddress, out _)
        && !string.IsNullOrWhiteSpace(GmailAppPassword));
}
