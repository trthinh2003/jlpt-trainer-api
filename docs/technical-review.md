# Review kỹ thuật JLPT Trainer Backend

Cơ sở review là source, migration và test trong workspace tại commit `3216c34`, ngày 18/11/2026. Tài liệu này tập trung vào chất lượng backend và các đề xuất phát triển. Kiến trúc, đường dẫn API và hành vi từng luồng được mô tả ở [Luồng nghiệp vụ](business-flows.md).

## Đề xuất ưu tiên

| Mức | Việc cần làm | Tiêu chí hoàn thành |
| --- | --- | --- |
| P0 | Phân quyền ghi kho học liệu chung | Người học đọc và ôn bình thường. Chỉ người quản lý kho được tạo, nhập và xóa. |
| P1 | Sửa dashboard, ghi lịch sử ôn và xử lý ghi đồng thời | Có dữ liệu thì cả hai endpoint dashboard trả đúng kết quả. Mỗi lượt ôn hợp lệ được ghi một lần và lịch ôn cập nhật cùng giao dịch. |
| P1 | Gắn bài nộp với phiên làm bài do server cấp | Câu hỏi và đáp án được cố định theo phiên. Mỗi phiên có một kết quả và lịch sử cũ vẫn đọc được. |
| P1 | Nâng .NET, dựng CI và siết giới hạn tài nguyên | Build và test chạy trên runtime được hỗ trợ. Kiểm tra dependency và giới hạn request được tự động hóa. |
| P2 | Đồng nhất import, phân trang và hợp đồng lỗi | Lỗi Excel chỉ đúng hàng, lỗi đầu vào nhất quán và trang có thứ tự ổn định. |

## Hiện trạng nghiệp vụ

Một người dùng đăng ký hoặc đăng nhập để nhận JWT, chọn học liệu chung, thêm vào bộ ôn cá nhân rồi chấm mức nhớ để hệ thống lên lịch. Người dùng cũng có thể lấy một bộ câu hỏi từ vựng theo cấp, nộp bài, xem lịch sử và xuất PDF. Dashboard ghép trạng thái thẻ với xu hướng điểm bài đã nộp. Hiện chưa có phiên làm bài giữa bước cấp đề và nộp, còn `StudySession` đã có bảng nhưng chưa có use case ghi dữ liệu.

Kiến trúc bốn project phù hợp với quy mô hiện tại. Nên giữ controller mỏng, các thư mục theo use case, `TimeProvider` ở logic thời gian, phép tính SRS thuần để kiểm thử và Testcontainers để kiểm tra PostgreSQL thật. Các handler học cá nhân hiện đã lọc theo user hoặc kiểm tra chủ tài nguyên. Những phần nên sửa trước nằm ở mô hình quyền kho chung, sự toàn vẹn của dữ liệu học và hợp đồng giữa các lớp.

## Phát hiện về bảo mật và độ tin cậy

### 1. P0 · Tài khoản thường có quyền thay đổi kho chung

Cả ba controller chỉ đặt `[Authorize]` ở cấp lớp. Các thao tác tạo, nhập và xóa dùng cùng điều kiện xác thực với thao tác đọc. Các handler xóa tìm thẻ theo loại và ID học liệu, rồi xóa thẻ của mọi người học đã lưu mục đó. Hệ quả là quyền ghi kho và khả năng làm mất tiến độ của người khác cùng nằm trong tài khoản người học. [Vocab](../src/JlptTrainer.Api/Controllers/VocabsController.cs#L41), [kanji](../src/JlptTrainer.Api/Controllers/KanjisController.cs#L32), [ngữ pháp](../src/JlptTrainer.Api/Controllers/GrammarPointsController.cs#L32), [xóa vocab và thẻ](../src/JlptTrainer.Application/Vocabs/Commands/DeleteVocab/DeleteVocabCommandHandler.cs#L18).

Áp policy quản lý kho cho cả ba nhóm thao tác ghi. Chỉ cấp quyền từ luồng quản trị đáng tin cậy và bao phủ hai vai trò trong integration test. Ưu tiên lưu trữ mục đã ngừng dùng nếu cần giữ tiến độ, kèm thông tin ai thay đổi học liệu và thời điểm thay đổi. Nếu muốn người dùng tự tạo từ riêng, thêm thư viện cá nhân có chủ sở hữu và policy tách biệt.

### 2. P0 · Cấu hình JWT phân tán trong lần review ban đầu

Luồng đổi mật khẩu đã thống nhất cấu hình ký và xác minh JWT qua `IOptions<JwtSettings>` và validate khi khởi động. Các kết quả bên dưới mô tả trạng thái trước bản sửa. [Program.cs](../src/JlptTrainer.Api/Program.cs#L44).

`Program` lấy cấu hình và giữ giá trị riêng cho điều kiện xác minh bearer. `JwtTokenGenerator` đọc `IOptions<JwtSettings>` do Infrastructure đăng ký. Integration factory bổ sung cấu hình JWT riêng. Hai đường đọc gây khó kiểm soát cấu hình hiệu lực khi test host thay provider. Lần chạy mặc định trả `401` khi test tạo vocab đã gửi token và bốn test khác thất bại khi đọc JSON từ response tạo vocab. Khi đặt cấu hình JWT của fixture ở cấp tiến trình test, cả 10 integration test đều đạt. Kết quả này khoanh vùng lỗi ở cách cấu hình JWT đi vào test host. [Xác minh JWT](../src/JlptTrainer.Api/Program.cs#L44), [đăng ký options](../src/JlptTrainer.Infrastructure/DependencyInjection.cs#L38), [ký token](../src/JlptTrainer.Infrastructure/Auth/JwtTokenGenerator.cs#L11), [cấu hình test](../tests/JlptTrainer.Api.IntegrationTests/CustomWebApplicationFactory.cs#L19).

Bind JWT options tập trung và dùng cùng nguồn cấu hình cho ký lẫn xác minh. Validate issuer, audience, signing key và expiry lúc khởi động, để cấu hình sai dừng trước khi request đăng ký ghi tài khoản. Giữ test tích hợp đi từ đăng ký đến gọi endpoint cần bearer. Các test setup nên kiểm tra mã HTTP trước khi deserialize để giữ lỗi gốc.

### 3. P1 · Bài luyện chưa có bằng chứng gắn kết quả với đề

Server chấm dựa trên nghĩa trong DB, nhưng nhận cấp, thời lượng và bộ câu trả lời từ client mà thiếu phiên phát đề để đối chiếu. Handler truy vấn theo ID vocab mà thiếu điều kiện cấp, tính điểm theo từng phần tử và coi ID lạ là sai. Validator chưa chặn câu trùng, chưa giới hạn số câu nộp, còn điểm và lịch sử có thể được ghi nhiều lần cho cùng một bộ câu trả lời. Hệ quả là kết quả hiện phù hợp với tự luyện, độ tin cậy chưa đủ cho đánh giá chính thức. [Handler chấm](../src/JlptTrainer.Application/MockTests/Commands/SubmitMockTest/SubmitMockTestCommandHandler.cs#L16), [validator](../src/JlptTrainer.Application/MockTests/Commands/SubmitMockTest/SubmitMockTestCommandValidator.cs#L7).

Tạo `MockTestAttempt` gắn user, cấp, thời điểm bắt đầu và snapshot câu hỏi cùng phương án. Client nộp ID phiên, câu và phương án. Server xác minh thành viên của đề, tính thời gian từ mốc server khi có quy định thời hạn và chuyển trạng thái nộp trong một giao dịch. Dùng unique constraint để bảo vệ kết quả theo phiên và thiết kế phản hồi khi client gửi lại. Các bài cũ tiếp tục đọc dưới dạng bản tổng kết. Nếu sau này dùng cho thi xếp hạng, cần ngân hàng câu hỏi tách với kho tra cứu công khai.

### 4. P1 · Thiếu giới hạn tài nguyên theo nghiệp vụ

`GetDueCards` đưa `MaxCards` thẳng vào `Take`, lịch sử bài luyện thiếu validator cho phân trang, và submit thiếu giới hạn số câu. API chưa cấu hình rate limiting tại `Program`, trong khi đăng nhập xác minh BCrypt và lookup đi qua HTTP tới Jisho. Import giới hạn request 5 MiB, sau đó sao chép file vào bộ nhớ và reader đọc theo kích thước sheet mà thiếu trần số dòng, cột hoặc dung lượng xử lý sau giải nén. Đây là rủi ro khả dụng ở tầng ứng dụng. Cấu hình reverse proxy và giới hạn môi trường triển khai cần được kiểm tra riêng. [Due handler](../src/JlptTrainer.Application/ReviewCards/Queries/GetDueCards/GetDueCardsQueryHandler.cs#L20), [lịch sử](../src/JlptTrainer.Application/MockTests/Queries/GetMockTestHistory/GetMockTestHistoryQueryHandler.cs#L15), [pipeline API](../src/JlptTrainer.Api/Program.cs#L68), [Excel reader](../src/JlptTrainer.Infrastructure/ExcelImport/EPPlusExcelReader.cs#L19).

Đặt trần số thẻ, trang và câu trả lời theo nhu cầu client. Giới hạn tần suất auth theo IP/tài khoản và các tác vụ nặng theo user, đo tỷ lệ từ chối để hiệu chỉnh. Giới hạn công việc giải nén và đọc workbook, trả lỗi 4xx có cấu trúc cho file sai định dạng hoặc vượt hạn mức. Khi quy định 5 MiB cho file, tính thêm phần overhead `multipart` ở giới hạn request.

### 5. P1 · Dashboard có sai lệch kiểu dữ liệu và thiếu nhật ký ôn

- **Ánh xạ SQL.** Migration trả `bigint` cho số đếm và `numeric` cho điểm trung bình. Npgsql đọc mặc định thành `long` và `decimal`, trong khi các record đích dùng `int` và `double`. `date` còn được Npgsql 9 đọc mặc định là `DateTime`, còn DTO dùng `DateOnly`. Dapper 2.1.35 cần constructor tương thích cho positional record. Suy luận từ kiểu và mã thư viện là cả hai endpoint có thể trả 500 khi query có dòng. Nên dùng kiểu SQL tương ứng trong các record nội bộ rồi chuyển sang kiểu API rõ ràng. Xác nhận trên PostgreSQL với dữ liệu thực sự có hàng. [Migration SQL](../src/JlptTrainer.Infrastructure/Migrations/20260802112353_AddDashboardFunctions.cs#L13), [DTO dashboard](../src/JlptTrainer.Application/Dashboard/Queries), [ánh xạ Npgsql](https://www.npgsql.org/doc/types/basic.html), [kiểu ngày của Npgsql](https://www.npgsql.org/doc/types/datetime), [Dapper 2.1.35](https://github.com/DapperLib/Dapper/blob/2.1.35/Dapper/DefaultTypeMap.cs#L66-L106).
- **Ý nghĩa heatmap.** Mỗi lượt ôn ghi đè `LastReviewedAt`. Một thẻ được ôn vào hai ngày chỉ đóng góp ngày gần nhất, và xóa thẻ xóa luôn phần đóng góp của thẻ. `days` lấy từ thời điểm hiện tại trừ số ngày rồi SQL lại nhóm theo ngày UTC, nên ngày đầu có thể chỉ được đếm một phần. Thêm lịch sử `ReviewEvent` theo từng lượt, dùng timezone đã thống nhất khi quy đổi ngày, và mô tả khoảng dữ liệu lịch sử còn giữ được. `StudySession` có thể dùng để gom nhiều sự kiện trong một phiên. [Ghi lượt ôn](../src/JlptTrainer.Application/ReviewCards/Commands/SubmitReview/SubmitReviewCommandHandler.cs#L30), [mốc thời gian](../src/JlptTrainer.Application/Dashboard/Queries/GetStudyHeatmap/GetStudyHeatmapQueryHandler.cs#L23), [StudySession](../src/JlptTrainer.Domain/Entities/StudySession.cs).

### 6. P1 · Ràng buộc đồng thời và quan hệ dữ liệu chưa đủ

- `SubmitReview` đọc trạng thái, tính lịch rồi ghi mà thiếu concurrency token. Hai request có thể cùng tính trên cùng bản cũ. Một lần gửi lại tuần tự có thể tăng `Repetitions` thêm lần nữa vì handler cho ôn sớm. Đặt version trên thẻ, idempotency key theo lượt và transaction cùng `ReviewEvent`. Chốt chính sách ôn sớm ở mức sản phẩm. [SubmitReview](../src/JlptTrainer.Application/ReviewCards/Commands/SubmitReview/SubmitReviewCommandHandler.cs#L18), [ReviewCard configuration](../src/JlptTrainer.Infrastructure/Persistence/Configurations/ReviewCardConfiguration.cs).
- Migration `StrengthenLearningDataConstraints` bổ sung FK từ ba bảng học tập đến `users` và các `CHECK` cho số nét, trạng thái SRS, số câu đúng và thời gian phiên. Liên kết `ReviewCard.ReferenceId` vẫn trỏ đa hình theo `CardType`. Kiểm tra tồn tại khi thêm thẻ có thể chạy song song với xóa học liệu rồi để lại thẻ mồ côi. Khi xử lý phần này, thống kê dữ liệu cũ, chọn khóa ngoại học liệu và xác định chính sách xóa cho lịch sử đã ghi. [Thêm thẻ](../src/JlptTrainer.Application/ReviewCards/Commands/AddToReview/AddToReviewCommandHandler.cs#L21), [migration ràng buộc dữ liệu](../src/JlptTrainer.Infrastructure/Migrations/20261118132437_StrengthenLearningDataConstraints.cs).
- Email, ký tự kanji và khóa duy nhất của thẻ đã được DB bảo vệ. Kiểm tra trùng rồi mới insert vẫn có thể ném lỗi unique khi hai request cùng ghi, và middleware hiện chuyển lỗi chưa biết thành 500. Ánh xạ vi phạm đúng constraint sang 409 hoặc kết quả idempotent tùy use case. Vocab và grammar thiếu unique constraint cho khóa mà import đang coi là định danh. Chốt quy tắc cho nhiều nghĩa hoặc mẫu trùng hợp lệ trước khi thêm uniqueness. [Register](../src/JlptTrainer.Application/Auth/Commands/Register/RegisterCommandHandler.cs#L20), [cấu hình EF](../src/JlptTrainer.Infrastructure/Persistence/Configurations), [middleware](../src/JlptTrainer.Api/Middleware/ExceptionHandlingMiddleware.cs#L23).

### 7. P1 · Nền tảng và cấu hình deploy

Repo target `net9.0`. Microsoft ghi nhận .NET 9 hết hỗ trợ ngày 10/11/2026. Nâng lên .NET 10 LTS cùng EF Core, provider PostgreSQL và các dependency tương thích. Kiểm lại migration, JWT, ánh xạ Dapper và Excel/PDF trên runtime đích. Repo đã quản lý version tập trung, thuận tiện để thêm kiểm tra dependency trực tiếp lẫn gián tiếp trong CI. [Target framework](../Directory.Build.props#L4), [version package](../Directory.Packages.props), [vòng đời .NET](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

`appsettings.json` được theo dõi hiện chỉ có cấu hình logging và host. File development đang bị ignore. Lần kiểm tra lịch sử của hai đường dẫn cấu hình chưa thấy signing key hoặc connection string trong revision theo dõi được kiểm tra. Môi trường triển khai cần cấp cấu hình bí mật riêng, CORS theo môi trường và validate cấu hình khi khởi động. Auth dùng cùng thông báo lỗi cho tài khoản không tồn tại và sai mật khẩu, nhưng đăng ký trả 409 khi email đã có. Cân nhắc mức tiết lộ trạng thái tồn tại theo thiết kế sản phẩm và áp giới hạn auth. [Git ignore](../.gitignore#L22), [Login](../src/JlptTrainer.Application/Auth/Commands/Login/LoginCommandHandler.cs#L19), [Register](../src/JlptTrainer.Application/Auth/Commands/Register/RegisterCommandHandler.cs#L20), [CORS](../src/JlptTrainer.Api/Program.cs#L70).

### 8. P1 · NuGet báo dependency gián tiếp có cảnh báo bảo mật

| Dependency đã resolve | Đường dẫn và xử lý |
| --- | --- |
| `System.Security.Cryptography.Pkcs` 8.0.1, High và Moderate | Có trong API qua Infrastructure → `EPPlus` 7.5.2. Ưu tiên cập nhật package gốc hoặc áp version đã vá tương thích, rồi chạy lại audit và test đọc/xuất Excel. [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx), [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7). |
| `SSH.NET` 2025.1.0, hai cảnh báo High | Có trong project integration test qua `Testcontainers.PostgreSql` → `Testcontainers`. Cập nhật chuỗi dependency test và kiểm tra test container chạy trên CI. [GHSA-q939-rpr3-3284](https://github.com/advisories/GHSA-q939-rpr3-3284), [GHSA-mggc-4xg6-vcxf](https://github.com/advisories/GHSA-mggc-4xg6-vcxf). |

Mức độ trên lấy từ NuGet advisory cho phiên bản đã resolve. Khả năng đường code của ứng dụng đi tới phần bị ảnh hưởng phụ thuộc vào cách dùng thư viện và môi trường triển khai.

## Những chỗ cần refactor

| Vị trí | Vấn đề và hướng xử lý |
| --- | --- |
| [Excel reader](../src/JlptTrainer.Infrastructure/ExcelImport/EPPlusExcelReader.cs#L24) và [vocab import](../src/JlptTrainer.Application/Vocabs/ImportVocabFromExcel/ImportVocabFromExcelCommandHandler.cs#L30) | Reader bỏ hàng trống nên `i + 2` báo sai số hàng sau khoảng trống. Header trùng có thể ghi đè giá trị. Trả record có số hàng vật lý, kiểm tra header một lần và tái sử dụng `JlptLevelParser` cho cả ba nhóm. Kiểm tra token level đơn lẻ thay vì parse tên enum tự do. |
| [Vocab import](../src/JlptTrainer.Application/Vocabs/ImportVocabFromExcel/ImportVocabFromExcelCommandHandler.cs#L35), [kanji import](../src/JlptTrainer.Application/Kanjis/Commands/ImportKanjiFromExcel/ImportKanjiFromExcelCommandHandler.cs#L29), [grammar import](../src/JlptTrainer.Application/GrammarPoints/Commands/ImportGrammarPointFromExcel/ImportGrammarPointFromExcelCommandHandler.cs#L29) | Import thiếu các hạn chế độ dài mà create hay schema áp dụng. Một dòng vượt độ dài có thể làm hỏng cả lần lưu. Chuẩn hóa sau trim, dùng chung quy tắc dữ liệu tạo và import, truy vấn khóa liên quan tới batch thay vì nạp cả kho, xử lý lỗi đua unique nhất quán. |
| [Sinh câu hỏi](../src/JlptTrainer.Application/MockTests/Queries/GetMockTestQuestions/GetMockTestQuestionsQueryHandler.cs#L16) | Handler nạp toàn bộ vocab theo cấp rồi xáo lại pool cho mỗi câu. Lựa chọn lọc theo ID nên có thể trùng nghĩa. Định nghĩa so trùng nghĩa theo quy tắc chấm, kiểm tra đủ lựa chọn phân biệt, giới hạn pool và lấy mẫu theo một chiến lược có thể kiểm thử. |
| [Phân trang vocab](../src/JlptTrainer.Application/Vocabs/Queries/GetVocabList/GetVocabListQueryHandler.cs#L20), [grammar](../src/JlptTrainer.Application/GrammarPoints/Queries/GetGrammarPointList/GetGrammarPointListQueryHandler.cs#L20) và [lịch sử bài](../src/JlptTrainer.Application/MockTests/Queries/GetMockTestHistory/GetMockTestHistoryQueryHandler.cs#L17) | Thứ tự chính có thể trùng nên trang thiếu thứ tự toàn phần. Thêm khóa phụ ID, giới hạn offset và kiểm tra tràn phép tính `Skip`. Chuyển sang cursor nếu lượng lịch sử khiến offset trở thành nút thắt. |
| [Validation behavior](../src/JlptTrainer.Application/Common/Behaviors/ValidationBehavior.cs#L12), [middleware](../src/JlptTrainer.Api/Middleware/ExceptionHandlingMiddleware.cs#L21) | Behavior dùng chung `ValidationContext` và chạy validator song song. Khi thêm nhiều validator dùng `DbContext` scoped, kiểm tra yêu cầu tuần tự. API hiện có cả lỗi MVC theo trường, `{ error }` của middleware và chuỗi từ import. Thống nhất `ProblemDetails` hoặc contract có mã lỗi, trường và trace ID. Tách client cancellation khỏi lỗi 500. |
| [Jisho client](../src/JlptTrainer.Infrastructure/ExternalServices/Jisho/JishoWordLookupService.cs#L14) | Lỗi HTTP upstream được chuyển cùng message tới client, còn lỗi JSON rơi vào 500 chung. Giữ chi tiết lỗi ở log, trả thông báo 502 ổn định, thêm cache theo từ khóa nếu đo được truy vấn lặp. |
| [Dashboard handlers](../src/JlptTrainer.Application/Dashboard/Queries), [DbContext interface](../src/JlptTrainer.Application/Common/Interfaces/IApplicationDbContext.cs), [EF CLI factory](../src/JlptTrainer.Infrastructure/Persistence/ApplicationDbContextFactory.cs#L10) | Application biết trực tiếp Dapper, SQL và `DbSet`. Giữ EF Core cho các use case đơn giản, đẩy dashboard SQL và mapping vào một read service khi sửa. Factory dò file theo working directory, nên chuẩn hóa cách truyền cấu hình hoặc đường dẫn và kiểm thử từ vị trí được hỗ trợ. |

## Hướng phát triển theo giai đoạn

1. **Ổn định trước khi mở cho nhiều người.** Giữ một backend phân module như hiện tại. Mở đầu bằng quyền quản lý kho, thống nhất JWT, nâng .NET và chạy CI với PostgreSQL. Thêm test cho đăng nhập đi tới route bảo vệ, phân quyền ba nhóm học liệu, sở hữu review/PDF và dashboard với dữ liệu. Bổ sung health check, log có trace ID và sao lưu PostgreSQL trước khi vận hành dữ liệu thật.
2. **Hoàn thiện vòng lặp học.** Ghi lịch sử ôn có idempotency, hiển thị heatmap theo múi giờ người học, hỗ trợ hoàn tác lượt chấm gần nhất nếu sản phẩm cần. Thêm phiên bài luyện, xem lại câu sai và thao tác thêm các mục sai vào bộ ôn. Đo tỷ lệ hoàn thành bài và tỷ lệ quay lại ôn đúng hạn để chọn tính năng tiếp theo.
3. **Đầu tư chất lượng nội dung.** Chuẩn hóa dữ liệu N5–N1, thêm nguồn và ví dụ, tìm kiếm theo chữ/đọc/nghĩa, import có xem trước và bản nháp trước khi xuất bản. Ưu tiên đủ nội dung N5/N4 và câu hỏi có lựa chọn phân biệt. Theo dõi số bản ghi thiếu nguồn, trùng khóa hoặc thiếu phương án trước khi mở rộng ngân hàng đề.
4. **Mở rộng khi luồng lõi đã ổn.** Thêm nghe và đọc hiểu với ngân hàng câu hỏi có phiên bản, đồng bộ tiến độ và nhắc lịch theo múi giờ. Chỉ tách background job cho những việc đã đo thấy ảnh hưởng độ trễ, như import lớn, báo cáo hoặc thông báo.

## Kiểm chứng trong workspace

- `dotnet test JlptTrainer.sln --nologo` build các project, unit test đạt 22/22. Integration test chạy bằng PostgreSQL 16 qua Testcontainers, đạt 5/10. Một test xác nhận nhận `401` khi kỳ vọng `201`; bốn test lỗi deserialize response rỗng ở bước tạo vocab. Bộ test hiện có bao phủ auth, vocab, review và một số handler. Chưa có integration test cho dashboard, mock test/PDF và import Excel thật.
- Chạy lại `dotnet test JlptTrainer.sln --no-restore --nologo` với các biến `Jwt__*` lấy từ cấu hình fixture và chỉ đặt trong tiến trình test. Unit test đạt 22/22 và integration test đạt 10/10. Đây là bước chẩn đoán cấu hình, bản sửa wiring JWT vẫn nằm trong đề xuất.
- `dotnet list JlptTrainer.sln package --vulnerable --include-transitive` đối chiếu NuGet và trả bốn advisory cho hai package. Dùng `dotnet nuget why` để kiểm tra đường dẫn dependency.
- Các phát hiện về quyền kho, hợp đồng chấm điểm, tải tài nguyên và đồng thời là review phòng thủ từ source. Kết quả kiểm thử hiện hữu không bao phủ hết các trường hợp này.
