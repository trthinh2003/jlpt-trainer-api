# JLPT Trainer バックエンド

[English](README.md) | **日本語** | [Tiếng Việt](README.vi.md)

JLPT N5〜N1 の学習を支援する ASP.NET Core API です。語彙・漢字・文法の教材、間隔反復による復習、語彙クイズ、結果の PDF 出力、学習進捗ダッシュボードを提供します。

学習画面は別リポジトリの [Vue フロントエンド](https://github.com/trthinh2003/jlpt-trainer-fe-vue/blob/master/README.ja.md)にあります。

## ローカルでの実行

.NET 9 SDK、PostgreSQL、`dotnet-ef` 9.x が必要です。開発用データベースを作成し、リポジトリのルートで PowerShell を開きます。以下のサンプルをローカル環境に合わせて変更してください。

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

マイグレーションはデータベーススキーマとダッシュボード用の SQL 関数を作成します。教材は API から追加するか、Excel テンプレートを使ってインポートできます。語彙クイズには、選択したレベルの語彙が 4 件以上必要です。

既存のデータベースを更新する際は、`StrengthenLearningDataConstraints` が既存データの制約違反を検証します。PostgreSQL から違反が報告された場合は、制約名で対象レコードを特定し、修正方法を確認してから再実行してください。主な確認対象は、存在しないユーザーを参照する `user_id`、画数が 0 以下の漢字、負の復習間隔や反復回数、問題数の範囲外の正答数、開始時刻より前に終了する学習セッションです。マイグレーションは制約を追加し、既存データを保持します。

新しいマイグレーションが追加されたら `./scripts/update-database.ps1` を再実行してください。パスワード変更に必要な `AddUserCredentialVersion` を含め、未適用のマイグレーションを順に適用します。接続設定は `ConnectionStrings__DefaultConnection` から取得し、未設定の場合は `src/JlptTrainer.Api/appsettings.Development.json` のローカル設定を使用します。既存アカウントの認証情報のバージョンは `0` から始まり、現在のパスワードを引き続き使えます。

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

## テスト

```powershell
dotnet test tests/JlptTrainer.Application.UnitTests
dotnet test JlptTrainer.sln
```

統合テストには、Linux コンテナを実行できる Docker が必要です。Testcontainers が PostgreSQL 16 のコンテナを起動し、マイグレーションを適用して、そのデータベースにテストデータを書き込みます。
