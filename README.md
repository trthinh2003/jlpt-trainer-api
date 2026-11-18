# JLPT Trainer Backend

**English** | [日本語](README.ja.md) | [Tiếng Việt](README.vi.md)

An ASP.NET Core API for studying Japanese at JLPT levels N5–N1. It provides vocabulary, kanji, and grammar catalogs, spaced-repetition review, vocabulary quizzes, PDF exports, and a progress dashboard.

The learner interface lives in the companion [Vue frontend](https://github.com/trthinh2003/jlpt-trainer-fe-vue/blob/master/README.md).

## Run locally

Install the .NET 9 SDK, PostgreSQL, and `dotnet-ef` 9.x. Create a development database. From the repository root in PowerShell, replace the example settings with your local values.

```powershell
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5432;Database=jlpttrainer_dev;Username=YOUR_USER;Password=YOUR_PASSWORD'
$jwtSecretBytes = New-Object byte[] 32
$jwtRandom = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $jwtRandom.GetBytes($jwtSecretBytes)
    $env:Jwt__Secret = [Convert]::ToBase64String($jwtSecretBytes)
} finally {
    $jwtRandom.Dispose()
}
$env:Jwt__Issuer = 'JlptTrainer.Api'
$env:Jwt__Audience = 'JlptTrainer.Client'
$env:Jwt__ExpiryMinutes = '60'

.\scripts\update-database.ps1

dotnet run --project src/JlptTrainer.Api --launch-profile https
```

Open Swagger at `https://localhost:7017/swagger`. If your browser does not trust the development certificate, run `dotnet dev-certs https --trust`. Register or sign in to get a JWT for protected endpoints. The API currently allows `http://localhost:4200` via CORS. The Vue development server uses a proxy.

Migrations create the schema and dashboard SQL functions. Add study material through the API or import it from an Excel template. Vocabulary quizzes require at least four items at the selected level.

When upgrading an existing database, `StrengthenLearningDataConstraints` checks existing rows. If PostgreSQL reports a violation, use the constraint name to identify the affected records and decide how to correct them before retrying. Check for orphaned `user_id` values, nonpositive kanji stroke counts, negative SRS intervals or repetition counts, correct-answer counts outside the question total, and study sessions that end before they start. The migration adds constraints and leaves your data in place.

Run `./scripts/update-database.ps1` whenever there are new migrations. The script applies pending migrations in order, including `AddUserCredentialVersion` for password changes. It reads the database connection from `ConnectionStrings__DefaultConnection`, falling back to `src/JlptTrainer.Api/appsettings.Development.json` for local configuration. Existing accounts start at credential version `0` and keep their passwords.

## Password reset with Gmail

1. Enable 2-Step Verification for the Gmail sender account and create an App Password. [Google's App Password guide](https://support.google.com/mail/answer/185833).
2. Add this section to `src/JlptTrainer.Api/appsettings.Development.json` alongside the existing settings. Git ignores this development file.

   ```json
   "PasswordReset": {
     "Enabled": true,
     "FrontendOrigin": "http://localhost:5173",
     "GmailAddress": "your-sender@gmail.com",
     "GmailAppPassword": "YOUR_GOOGLE_APP_PASSWORD"
   }
   ```

3. From the backend repository root, run `./scripts/update-database.ps1` to apply `AddPasswordResetFields`, then restart the API. Open **Quên mật khẩu?** on the Vue sign-in page and enter the email for the JLPT Trainer account you want to recover. `GmailAddress` is the sender's mailbox.

The API sends mail through Gmail SMTP over STARTTLS on port 587. For deployment, set `FrontendOrigin` to the Vue app's HTTPS origin and supply `PasswordReset__GmailAppPassword` through a secret manager as an environment variable. The API queues email in memory. If the API restarts before a message is sent, users can request a fresh link. [Gmail SMTP settings](https://support.google.com/mail/answer/7104828).

## Tests

```powershell
dotnet test tests/JlptTrainer.Application.UnitTests
dotnet test JlptTrainer.sln
```

Integration tests require Docker with Linux containers. Testcontainers starts PostgreSQL 16, applies migrations, and writes test data to the container database.
