# JLPT Trainer バックエンド

[English](README.md) | **日本語** | [Tiếng Việt](README.vi.md)

JLPT N5〜N1 の学習を支援する ASP.NET Core API です。語彙・漢字・文法の教材、間隔反復による復習、語彙クイズ、結果の PDF 出力、学習進捗ダッシュボードを提供します。

学習画面は別リポジトリの [Vue フロントエンド](https://github.com/trthinh2003/jlpt-trainer-fe-vue/blob/master/README.ja.md)にあります。

## ローカルでの実行

.NET 10 SDK、PostgreSQL、`dotnet-ef` 10.x が必要です。開発用データベースを作成し、リポジトリのルートで PowerShell を開きます。以下のサンプルをローカル環境に合わせて変更してください。

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

Swagger は `https://localhost:7017/swagger` で開けます。ブラウザーが開発用証明書を信頼していない場合は、`dotnet dev-certs https --trust` を実行してください。アカウント登録またはログインで JWT を取得すると、認証が必要な API を利用できます。API の CORS 設定では `http://localhost:4200` を許可しています。Vue の開発サーバーはプロキシ経由で接続します。

マイグレーションはデータベーススキーマとダッシュボード用の SQL 関数を作成します。教材は API から追加するか、Excel テンプレートを使ってインポートできます。語彙クイズには、選択したレベルに異なる意味が 4 種類以上必要です。

新しい API を起動する前に、`./scripts/update-database.ps1` で `AddMockTestAttempts` を適用します。`POST /api/mocktests/attempts` に `level` と `questionCount` を送ると、制限時間 15 分のクイズを開始できます。`GET /api/mocktests/attempts/{id}` で再開し、`POST /api/mocktests/attempts/{id}/submit` に各問題の選択肢番号（0〜3、未回答は `null`）を `selectedChoices` として送信します。返された結果 ID は `GET /api/mocktests/{id}` で使います。旧提出 API は HTTP 410 を返します。過去の結果は履歴と PDF で引き続き確認できます。

既存環境で学習進捗画面を有効にするには、API を停止し、`./scripts/update-database.ps1` で `AddReviewEvents` を適用してから、新しい API を起動します。各カードの最後の復習日時が履歴に移行され、その後は評価ごとに 1 件記録されます。同じカードと評価を再送する場合は、任意の UUID `requestId` を再利用します。`GET /api/dashboard/heatmap` は UTC 日付別の復習回数、`GET /api/dashboard/progress` はログインユーザーの習熟度とクイズの平均点を返します。

`LinkQuizQuestionsToVocabulary` の適用後に作成したクイズでは、誤答・未回答の語彙を復習に追加できます。元の語彙を教材から削除しても、履歴には出題された問題が残ります。

既存のデータベースを更新する際は、`StrengthenLearningDataConstraints` が既存データの制約違反を検証します。PostgreSQL から違反が報告された場合は、制約名で対象レコードを特定し、修正方法を確認してから再実行してください。主な確認対象は、存在しないユーザーを参照する `user_id`、画数が 0 以下の漢字、負の復習間隔や反復回数、問題数の範囲外の正答数、開始時刻より前に終了する学習セッションです。マイグレーションは制約を追加し、既存データを保持します。

新しいマイグレーションが追加されたら `./scripts/update-database.ps1` を再実行してください。パスワード変更に必要な `AddUserCredentialVersion` を含め、未適用のマイグレーションを順に適用します。接続設定は `ConnectionStrings__DefaultConnection` から取得し、未設定の場合は `src/JlptTrainer.Api/appsettings.Development.json` のローカル設定を使用します。既存アカウントの認証情報のバージョンは `0` から始まり、現在のパスワードを引き続き使えます。

ログインした学習者は `POST /api/reviewcards/batch` で `cardType`（語彙は 0、漢字は 1、文法は 2）と同じ種類の教材 ID を 1〜20 件含む `referenceIds` を送信できます。重複を除いた教材 ID は、新規追加の `addedIds`、登録済みの `alreadyAddedIds`、見つからない `missingIds` に分けて返ります。再送しても既存カードの復習予定は保持されます。`POST /api/reviewcards/kanji/batch` では従来どおり `referenceIds` に漢字 ID を指定できます。

`GET /api/reviewcards/due` では、`cardType`（語彙は 0、漢字は 1、文法は 2）と `level`（1〜5）を任意で指定できます。ログインした学習者の復習期限を迎えたカードを絞り込んでから、既定値が 20 の `maxCards` を適用します。

## Gmail でパスワードを再設定

1. 送信用 Gmail アカウントで 2 段階認証を有効にし、アプリ パスワードを作成します。[Google の設定ガイド](https://support.google.com/mail/answer/185833)
2. 以下のセクションを `src/JlptTrainer.Api/appsettings.Development.json` の既存設定に追加します。この開発用ファイルは Git の追跡対象外です。

   ```json
   "PasswordReset": {
     "Enabled": true,
     "FrontendOrigin": "http://localhost:5173",
     "GmailAddress": "your-sender@gmail.com",
     "GmailAppPassword": "YOUR_GOOGLE_APP_PASSWORD"
   }
   ```

3. バックエンドのルートから `./scripts/update-database.ps1` を実行して `AddPasswordResetFields` を適用し、API を再起動します。Vue のログイン画面で **Quên mật khẩu?**（パスワードをお忘れですか？）を選び、再設定したい JLPT Trainer アカウントのメールアドレスを入力します。`GmailAddress` は送信用のメールアドレスです。

API は Gmail の SMTP サーバーにポート 587 の STARTTLS で接続します。デプロイ時は `FrontendOrigin` に Vue アプリの HTTPS オリジンを設定し、`PasswordReset__GmailAppPassword` をシークレット管理システムから環境変数として渡します。メールは API プロセス内のメモリキューに入ります。送信前に API が再起動した場合は、再設定リンクをもう一度リクエストできます。[Gmail の SMTP 設定](https://support.google.com/mail/answer/7104828)

## 教材の管理

API の起動前に `./scripts/update-database.ps1` を実行し、`AddUserRole` を適用します。既存のアカウントと新規登録アカウントには、既定で学習者の権限が割り当てられます。ログインしたユーザーは教材を閲覧し、自分の復習カードを管理できます。教材管理者は、共有の語彙・漢字・文法の追加、インポート、削除を行えます。`GET /api/auth/me` は教材管理者には `{ "canManageCatalog": true }`、学習者には `false` を返します。

`GET /api/vocabs` では、最大100文字の `search` を指定して、表記や読みを部分一致で検索できます。`level`、`pageNumber`、`pageSize` も併用できます。教材管理者は `PUT /api/vocabs/{id}/content` で `meaning`、`exampleSentence`、`exampleSentenceMeaning` を更新できます。例文や訳文を消すには `null` を送ります。更新に成功すると `204` が返り、語彙の ID と既存の復習カードは保持されます。

`GET /api/kanjis` の `search` は漢字、音読み、訓読み、意味を検索し、`GET /api/grammarpoints` では文型と意味を検索します。教材管理者は `PUT /api/kanjis/{id}/content` で `meaning`、`onYomi`、`kunYomi` を更新できます。`PUT /api/grammarpoints/{id}/content` では `meaning`、`exampleSentence`、`exampleSentenceMeaning` を更新できます。成功時は `204` が返り、既存の復習カードにも更新が反映されます。

Vue では Excel ファイルを選び、プレビューで結果を確認してからインポートを確定します。語彙・漢字・文法の教材管理者は、`POST /api/{catalog}/import/preview` に multipart の `file` を送信できます。レスポンスには `totalRows`、`readyCount`、`skippedDuplicateCount`、Excel の行番号付きの `errors` が含まれます。プレビューでは教材を保存しません。`POST /api/{catalog}/import` は最新の教材データを使ってファイルを再検証し、有効な新規行を保存して、実際の保存件数を `successCount` で返します。

語彙の追加やインポートを使う前に `./scripts/update-database.ps1` で `EnforceUniqueVocabKeys` を適用してください。表記と読みが重複する既存語彙がある場合は、関連する復習カードやクイズの参照を含めて確認してから適用します。重複する語彙の追加は `409` を返し、Excel インポートでは該当行をスキップ件数に含めます。

重複を確認した後、データベースをバックアップして API を停止します。`./scripts/update-database.ps1 -MergeIdenticalVocabs` を実行すると、内容が一致する語彙の統合とインデックスの作成が同じトランザクションで行われます。意味、例文、例文の訳、レベルが異なる項目は個別に確認してください。同じ学習者のカードが重複する場合は、最後に復習したカードのスケジュールを採用します。

既存のアカウントに教材管理の権限を付与するには、対象の PostgreSQL データベースで、データベース管理者として以下の SQL を実行します。メールアドレスは対象アカウントの登録アドレスに置き換えてください。権限を取り消すには `role` を `0` に戻します。

```sql
UPDATE users SET role = 1 WHERE email = lower('manager@example.com');
```

### 語彙の画像

`./scripts/update-database.ps1` で `AddVocabImages` を適用します。API に `Media__S3__Bucket` と `Media__S3__Region` を設定します。実行環境の IAM ロール、またはローカルの `AWS_PROFILE` を使用します。非公開バケットの `media/vocab-images/*` に `s3:GetObject`、`s3:PutObject`、`s3:DeleteObject` を付与します。DB 環境ごとにバケットを分け、バージョニングを有効にする場合は旧バージョンの有効期限を設定します。

管理者は `PUT /api/vocabs/{id}/image` に multipart の `file` を送信し、`DELETE /api/vocabs/{id}/image` で画像を解除します。JPEG、PNG、WebP の上限は 5 MB、2048 × 2048 px です。語彙 API は `hasImage` を返します。ログイン済みユーザーは `GET /api/vocabs/{id}/image` で有効期限 5 分の URL を取得できます。画像がない場合は `204` です。

### 動画レッスン

`AddVideoLessons` を適用します。語彙画像と同じ S3 設定を使用し、`media/video-lessons/*` にオブジェクトの読み取り・書き込み・削除権限を付与します。リバースプロキシを使用する場合は、50 MB の動画と multipart の付加データを受け取れるように設定します。

管理者は `POST /api/video-lessons` に multipart の `title`、`description`、`level`、`file` を送信し、`DELETE /api/video-lessons/{id}` で公開を取り消せます。ログイン済みユーザーは一覧と `GET /api/video-lessons/{id}/playback` を利用できます。再生 URL の有効期限は 30 分です。H.264/AAC の MP4 を推奨します。

### 音声レッスン

`AddAudioLessons` を適用し、API に `media/audio-lessons/*` の読み取り・書き込み・削除権限を付与します。管理者は音声（MP3 または WAV、最大 25 MB）、画像（JPEG、PNG、WebP、最大 5 MB と 2048 × 2048 px）、タイムスタンプ付き LRC と語彙 1〜20 件を登録します。学習者は再生に合わせて歌詞を読み、再生後に語彙を復習リストへ追加できます。

公開前に音声ファイルを選び、LRC を入力して **Xem trước lời** を押すと同期を確認できます。管理者向けの `POST /api/audio-lessons/preview-lyrics` は JSON の `lyrics` を受け取り、時刻順に並んだ `{ startMs, text }` を返します。ファイルはブラウザーでローカル再生します。

## テスト

```powershell
dotnet test tests/JlptTrainer.Application.UnitTests
dotnet test JlptTrainer.sln
```

統合テストには、Linux コンテナを実行できる Docker が必要です。Testcontainers が PostgreSQL 16 のコンテナを起動し、マイグレーションを適用して、そのデータベースにテストデータを書き込みます。
