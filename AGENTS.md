# Làm việc với JLPT Trainer Backend

- Từ root, dùng `dotnet build JlptTrainer.sln` và `dotnet test JlptTrainer.sln`. Integration test dùng Testcontainers với PostgreSQL 16 và cần Docker. Nếu Docker không chạy được, chạy riêng `dotnet test tests/JlptTrainer.Application.UnitTests` và báo rõ phần còn thiếu.
- Lần theo một tính năng từ controller đến MediatR request, validator, handler và persistence. Đọc [luồng nghiệp vụ](docs/business-flows.md) và đối chiếu với code trước khi đổi hành vi.
- Đặt HTTP binding, status và authorization policy ở API. Đặt xử lý use case và validation trong Application theo thư mục tính năng. Dùng Infrastructure cho EF Core, Dapper, JWT, Excel, PDF và client HTTP. Giữ Domain độc lập với các project còn lại.
- Lấy user hiện tại qua `ICurrentUserService`. Khi sửa luồng học cá nhân, kiểm tra quyền sở hữu ở thao tác đọc và ghi. Khi sửa học liệu, xác định chính sách cho kho dùng chung và ảnh hưởng đến thẻ ôn của người học.
- Dùng `TimeProvider` cho logic theo thời gian. Khi sửa lịch ôn hoặc dashboard, kiểm tra mốc ngày, múi giờ và cách tính thống kê.
- Khi đổi schema hoặc SQL function của dashboard, thêm migration tương ứng và kiểm tra trên PostgreSQL. Dùng `scripts/update-database.ps1` để áp migration. Khi gọi EF CLI trực tiếp, chạy từ `src/JlptTrainer.Infrastructure` vì design-time factory dùng đường dẫn tương đối. [README](README.md).
- Dùng cấu hình local hoặc biến môi trường cho JWT và PostgreSQL. Tránh đưa token, signing key và mật khẩu database vào commit, tài liệu hoặc log.
- Khi đổi API contract, cập nhật tài liệu và các test liên quan. [Review kỹ thuật](docs/technical-review.md) ghi các đề xuất chưa triển khai.

## Khi sửa từng luồng

- Pipeline bearer và `JwtTokenGenerator` dùng chung `JwtSettings`. Khi sửa auth, kiểm tra token do API phát gọi được route có `[Authorize]`, và các token cũ bị từ chối sau khi đổi mật khẩu.
- Ba nhóm học liệu dùng kho chung. Khi sửa quyền quản lý kho, bao phủ cả tạo, nhập Excel và xóa. Khi sửa xóa, kiểm tra ảnh hưởng lên thẻ ôn của người khác và thao tác thêm thẻ đồng thời.
- Khi đổi import Excel, dùng chung quy tắc dữ liệu với luồng tạo trực tiếp. Giữ số dòng vật lý của workbook trong lỗi trả về và kiểm tra khóa trùng ở PostgreSQL.
- Khi đổi SRS, kiểm tra gửi lại request và hai lần ghi đồng thời. Khi đổi heatmap, kiểm tra một thẻ được ôn nhiều lần và dữ liệu sát ranh giới ngày.
- Khi đổi bài luyện, kiểm tra đề, quyền sở hữu, thời gian và trạng thái nộp. Nếu thêm phiên làm bài, giữ khả năng đọc lịch sử và PDF của các bản tổng kết cũ.
