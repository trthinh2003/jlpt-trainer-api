# JLPT Trainer Backend

[English](README.md) | [日本語](README.ja.md) | **Tiếng Việt**

API học tiếng Nhật theo cấp N5–N1. Repo gồm kho từ vựng, kanji và ngữ pháp, ôn tập theo lịch SRS, bài luyện từ vựng, xuất PDF và dashboard tiến độ.

Giao diện học nằm ở repo [Vue frontend](https://github.com/trthinh2003/jlpt-trainer-fe-vue/blob/master/README.vi.md).

## Chạy local

Cần .NET SDK 9, PostgreSQL và công cụ `dotnet-ef` 9.x. Tạo database phát triển riêng. Trong PowerShell, từ thư mục gốc repo, thay các giá trị mẫu bằng cấu hình local của bạn.

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

Mở Swagger tại `https://localhost:7017/swagger`. Nếu trình duyệt chưa tin chứng chỉ local, chạy `dotnet dev-certs https --trust`. Đăng ký hoặc đăng nhập để lấy JWT dùng cho những API còn lại. CORS hiện cho phép client tại `http://localhost:4200`.

Migration tạo schema và các SQL function của dashboard. Kho học liệu có thể được nhập từ template Excel hoặc thêm qua API. Bài luyện từ vựng yêu cầu tối thiểu bốn mục ở cấp đã chọn.

Khi nâng cấp database đã có dữ liệu, migration `StrengthenLearningDataConstraints` xác thực cả dữ liệu cũ. Nếu PostgreSQL báo vi phạm ràng buộc, dùng tên constraint để tìm bản ghi liên quan và xác minh cách xử lý trước khi chạy lại. Các trường hợp cần kiểm tra gồm `user_id` mồ côi, số nét kanji không dương, trạng thái SRS âm, số câu đúng vượt tổng số câu và phiên học kết thúc trước lúc bắt đầu. Migration thêm ràng buộc và giữ nguyên dữ liệu.

Chạy lại `.\scripts\update-database.ps1` khi repo có migration mới. Script áp tất cả migration còn thiếu theo thứ tự, gồm `AddUserCredentialVersion` cần cho tính năng đổi mật khẩu. Nó đọc kết nối từ `ConnectionStrings__DefaultConnection` hoặc `src/JlptTrainer.Api/appsettings.Development.json` khi biến môi trường chưa được đặt. Tài khoản cũ bắt đầu ở phiên bản `0` và giữ mật khẩu hiện tại.

## Quên mật khẩu qua Gmail

1. Bật xác minh hai bước cho tài khoản Gmail dùng để gửi rồi tạo App Password. [Hướng dẫn của Google](https://support.google.com/mail/answer/185833).
2. Thêm khối sau vào `src/JlptTrainer.Api/appsettings.Development.json`, giữ các mục cấu hình hiện có. File development đã được Git ignore.

   ```json
   "PasswordReset": {
     "Enabled": true,
     "FrontendOrigin": "http://localhost:5173",
     "GmailAddress": "your-sender@gmail.com",
     "GmailAppPassword": "YOUR_GOOGLE_APP_PASSWORD"
   }
   ```

3. Từ thư mục `backend`, chạy `.\scripts\update-database.ps1` để áp migration `AddPasswordResetFields`, rồi khởi động lại API. Mở **Quên mật khẩu?** trên trang đăng nhập của Vue. Nhập email của tài khoản JLPT Trainer muốn khôi phục. `GmailAddress` là hộp thư gửi.

Backend dùng Gmail SMTP qua STARTTLS ở cổng 587. Khi triển khai, đặt `FrontendOrigin` thành origin HTTPS của Vue và cấp `PasswordReset__GmailAppPassword` qua biến môi trường từ hệ thống quản lý bí mật. Email được gửi qua hàng đợi trong tiến trình API. Nếu API khởi động lại trước khi gửi, người dùng có thể yêu cầu link mới. [Cấu hình SMTP Gmail](https://support.google.com/mail/answer/7104828).

## Kiểm thử

```powershell
dotnet test tests/JlptTrainer.Application.UnitTests
dotnet test JlptTrainer.sln
```

Integration test cần Docker chạy Linux containers. Testcontainers khởi tạo PostgreSQL 16, áp migration và ghi dữ liệu test vào database của container.
