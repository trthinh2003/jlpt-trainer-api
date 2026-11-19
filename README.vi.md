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

Để mở dashboard trên bản cài đặt hiện có, dừng API, chạy `./scripts/update-database.ps1` để áp dụng `AddReviewEvents`, rồi khởi động bản API mới. Migration khôi phục lần ôn gần nhất còn lưu trên mỗi thẻ. Từ đó mỗi lượt chấm tạo một bản ghi lịch sử. Khi gửi lại cùng thẻ và mức chấm, dùng lại UUID `requestId` tùy chọn. `GET /api/dashboard/heatmap` đếm lượt ôn theo ngày UTC. `GET /api/dashboard/progress` trả mức độ ghi nhớ và điểm bài luyện trung bình của người đăng nhập.

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

## Quản lý kho học liệu

Chạy `./scripts/update-database.ps1` để áp `AddUserRole` trước khi khởi động API. Tài khoản có sẵn và tài khoản mới mặc định là người học. Mọi tài khoản đã đăng nhập có thể xem kho và quản lý bộ ôn cá nhân. Quản lý kho được tạo, nhập và xóa từ vựng, kanji và ngữ pháp dùng chung. `GET /api/auth/me` trả `{ "canManageCatalog": true }` cho quản lý kho và `false` cho người học.

Để cấp quyền cho tài khoản đã đăng ký, chạy SQL sau bằng tài khoản quản trị trên đúng database PostgreSQL. Dùng email của tài khoản cần cấp quyền. Để thu hồi, đặt lại `role` về `0`.

```sql
UPDATE users SET role = 1 WHERE email = lower('manager@example.com');
```

## Kiểm thử

```powershell
dotnet test tests/JlptTrainer.Application.UnitTests
dotnet test JlptTrainer.sln
```

Integration test cần Docker chạy Linux containers. Testcontainers khởi tạo PostgreSQL 16, áp migration và ghi dữ liệu test vào database của container.
