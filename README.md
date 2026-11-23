# JLPT Trainer Backend

**English** | [日本語](README.ja.md) | [Tiếng Việt](README.vi.md)

An ASP.NET Core API for studying Japanese at JLPT levels N5–N1. It provides vocabulary, kanji, and grammar catalogs, spaced-repetition review, vocabulary quizzes, PDF exports, and a progress dashboard.

The learner interface lives in the companion [Vue frontend](https://github.com/trthinh2003/jlpt-trainer-fe-vue/blob/master/README.md).

## Run locally

Install the .NET 10 SDK, PostgreSQL, and `dotnet-ef` 10.x. Create a development database. From the repository root in PowerShell, replace the example settings with your local values.

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

Migrations create the schema and dashboard SQL functions. Add study material through the API or import it from an Excel template. Vocabulary quizzes require at least four distinct meanings at the selected level.

Apply `AddMockTestAttempts` with `./scripts/update-database.ps1` before starting the updated API. Create a 15-minute vocabulary quiz with `POST /api/mocktests/attempts` using `level` and `questionCount`. Resume with `GET /api/mocktests/attempts/{id}`. Submit with `POST /api/mocktests/attempts/{id}/submit` using one `selectedChoices` entry per question (0–3 or `null` for skipped questions). The response is the result ID for `GET /api/mocktests/{id}`. The previous submit route returns HTTP 410. Existing results remain available in history and as PDFs.

To enable the study dashboard on an existing installation, stop the API, run `./scripts/update-database.ps1` to apply `AddReviewEvents`, then restart with the updated API. The migration restores the last recorded review for each existing card. Each new grade creates a history entry. Reuse the optional `requestId` UUID when retrying a review submission with the same card and grade. `GET /api/dashboard/heatmap` counts reviews by UTC day, and `GET /api/dashboard/progress` returns card mastery and quiz averages for the signed-in user.

The `LinkQuizQuestionsToVocabulary` migration enables adding missed or skipped vocabulary to spaced repetition for newly issued quizzes. Quiz history keeps its original questions even if a vocabulary item is later removed.

When upgrading an existing database, `StrengthenLearningDataConstraints` checks existing rows. If PostgreSQL reports a violation, use the constraint name to identify the affected records and decide how to correct them before retrying. Check for orphaned `user_id` values, nonpositive kanji stroke counts, negative SRS intervals or repetition counts, correct-answer counts outside the question total, and study sessions that end before they start. The migration adds constraints and leaves your data in place.

Run `./scripts/update-database.ps1` whenever there are new migrations. The script applies pending migrations in order, including `AddUserCredentialVersion` for password changes. It reads the database connection from `ConnectionStrings__DefaultConnection`, falling back to `src/JlptTrainer.Api/appsettings.Development.json` for local configuration. Existing accounts start at credential version `0` and keep their passwords.

Signed-in learners can send `POST /api/reviewcards/batch` with `cardType` (0 for vocabulary, 1 for Kanji, 2 for grammar) and `referenceIds`, an array of 1 to 20 IDs from that catalog. The response groups the unique reference IDs into `addedIds`, `alreadyAddedIds`, and `missingIds`. Retrying preserves the schedule of each existing card. `POST /api/reviewcards/kanji/batch` also accepts Kanji IDs with the original `referenceIds` request body.

`GET /api/reviewcards/due` accepts optional `cardType` (0 for vocabulary, 1 for Kanji, 2 for grammar) and `level` (1 to 5). It filters the signed-in learner's due cards before applying `maxCards`, which defaults to 20.

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

## Catalog management

Run `./scripts/update-database.ps1` to apply `AddUserRole` before starting the API. Existing and newly registered accounts are learners by default. Any signed-in learner can browse the catalog and manage their own review cards. A catalog manager can create, import, and delete shared vocabulary, kanji, and grammar. `GET /api/auth/me` returns `{ "canManageCatalog": true }` for managers and `false` for learners.

`GET /api/vocabs` accepts an optional `search` of up to 100 characters to match part of the word or reading. Combine it with `level`, `pageNumber`, and `pageSize`. Catalog managers can update a word's meaning and examples through `PUT /api/vocabs/{id}/content` with `meaning`, `exampleSentence`, and `exampleSentenceMeaning`. Send `null` to clear an example field. Success returns `204`. The word's ID and existing review cards are preserved.

`GET /api/kanjis` also accepts `search` to match the character, On or Kun readings, or meaning. `GET /api/grammarpoints` matches the pattern or meaning. Catalog managers can call `PUT /api/kanjis/{id}/content` with `meaning`, `onYomi`, and `kunYomi`, or `PUT /api/grammarpoints/{id}/content` with `meaning`, `exampleSentence`, and `exampleSentenceMeaning`. Both return `204` on success and update the content shown on existing review cards.

In Vue, open **Import Excel**, select a workbook, and preview it before confirming. For vocabulary, kanji, and grammar, catalog managers can send the same multipart `file` to `POST /api/{catalog}/import/preview` to get `totalRows`, `readyCount`, `skippedDuplicateCount`, and `errors` with Excel row numbers. The preview leaves the catalog unchanged. `POST /api/{catalog}/import` checks the workbook against the current catalog, writes valid new rows, and reports the actual `successCount`.

Apply `EnforceUniqueVocabKeys` with `./scripts/update-database.ps1` before using vocabulary creation and import. Review any existing duplicate word and reading pairs and their review card and quiz links before applying the migration. Creating a duplicate returns `409`. Excel import counts duplicate rows as skipped.

After reviewing duplicate pairs, back up the database and stop the API. Run `./scripts/update-database.ps1 -MergeIdenticalVocabs` to merge identical entries and create the index in one transaction. Differences in meaning, examples or level require manual review. If a learner has multiple cards for the same word, the most recently reviewed card takes precedence.

To grant catalog management to an existing account, run the following SQL as a database administrator in the intended PostgreSQL database. Use the account's registered email. To revoke access, set `role` back to `0`.

```sql
UPDATE users SET role = 1 WHERE email = lower('manager@example.com');
```

### Vocabulary images

Apply `AddVocabImages` with `./scripts/update-database.ps1`. To enable S3, set `Media__S3__Bucket` and `Media__S3__Region` for the API. Use an AWS role on the host or set `AWS_PROFILE` to an authorized local profile. The private bucket grants `s3:GetObject`, `s3:PutObject`, and `s3:DeleteObject` on `media/vocab-images/*`. Use one bucket per database environment. For a versioned bucket, configure expiration of noncurrent versions to reclaim their storage.

Managers upload a multipart `file` with `PUT /api/vocabs/{id}/image` or detach it with `DELETE /api/vocabs/{id}/image`. Uploads accept JPEG, PNG and WebP up to 5 MB and 2048 × 2048 px. Vocabulary responses include `hasImage`. Signed-in learners call `GET /api/vocabs/{id}/image` to get `{ "url": "..." }` valid for five minutes or `204` when there is no image. The API re-encodes uploads and removes unused files in the background.

### Video lessons

Apply `AddVideoLessons` to enable the video catalog. This uses the vocabulary image S3 settings. The API's IAM principal needs object read, write, and delete permissions on `media/video-lessons/*`. If using a reverse proxy, configure the request limit to allow a 50 MB video plus multipart overhead and set timeouts suitable for uploads.

Managers publish an MP4 with multipart `title`, `description`, `level`, and `file` at `POST /api/video-lessons` and remove it with `DELETE /api/video-lessons/{id}`. Signed-in users browse `GET /api/video-lessons` with `level`, `pageNumber`, and `pageSize`. They request a 30-minute playback URL from `GET /api/video-lessons/{id}/playback`. Browsers play and seek directly on S3. H.264/AAC in an MP4 container is recommended.

### Audio lessons

Apply `AddAudioLessons` and grant the API object read, write, and delete permissions on `media/audio-lessons/*`.

To check timing before publishing, managers select a local audio file, enter LRC and click **Xem trước lời** in Vue. `POST /api/audio-lessons/preview-lyrics` accepts JSON with a `lyrics` string and returns sorted `{ startMs, text }` cues. Vue plays the files locally, and the endpoint performs no database writes or S3 uploads.

Managers publish a lesson through multipart `POST /api/audio-lessons` with `title`, `level`, `audio` (MP3 or WAV up to 25 MB), `cover` (JPEG, PNG or WebP up to 5 MB and 2048 × 2048 px), `lyrics` (timed LRC text), and repeated `vocabIds` fields for 1–20 catalog words. They unpublish it with `DELETE /api/audio-lessons/{id}`. Signed-in learners can list lessons and request their playback or vocabulary via the `playback` and `words` endpoints.

## Tests

```powershell
dotnet test tests/JlptTrainer.Application.UnitTests
dotnet test JlptTrainer.sln
```

Integration tests require Docker with Linux containers. Testcontainers starts PostgreSQL 16, applies migrations, and writes test data to the container database.
