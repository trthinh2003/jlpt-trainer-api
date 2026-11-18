using JlptTrainer.Domain.Common;

namespace JlptTrainer.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public int CredentialVersion { get; private set; }

        public string? PasswordResetTokenHash { get; private set; }

        public DateTimeOffset? PasswordResetExpiresAt { get; private set; }

        public string DisplayName { get; set; } = string.Empty;

        public void ChangePassword(string passwordHash)
        {
            PasswordHash = passwordHash;
            CredentialVersion++;
            PasswordResetTokenHash = null;
            PasswordResetExpiresAt = null;
        }

        public void SetPasswordResetToken(string tokenHash, DateTimeOffset expiresAt)
        {
            PasswordResetTokenHash = tokenHash;
            PasswordResetExpiresAt = expiresAt;
        }
    }
}
