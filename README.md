# Sổ Nhỏ — Quản lý thu chi cá nhân

Ứng dụng desktop WPF trên .NET 8 để ghi chép thu nhập, chi tiêu theo danh mục, xem tổng quan tháng và xuất báo cáo Excel.

## Chức năng

- Ghi nhận khoản thu hoặc chi với số tiền, ngày, danh mục và ghi chú.
- Xem giao dịch theo tháng; tổng hợp thu nhập, chi tiêu, số dư và biểu đồ tỷ trọng chi.
- Xóa giao dịch và xuất danh sách tháng đang xem thành `.xlsx`.
- Đặt ngân sách theo danh mục/tháng; ứng dụng cảnh báo khi chi tiêu đạt 80% hạn mức hoặc vượt hạn mức.
- Phân loại giao dịch theo ví/tài khoản như tiền mặt, ngân hàng, MoMo và ZaloPay; có thể thêm ví mới.
- Lập giao dịch định kỳ hàng tháng; ứng dụng tự ghi các kỳ đến hạn khi mở và kiểm tra định kỳ khi đang chạy.
- Lưu dữ liệu trong SQL Server bằng Entity Framework Core; các danh mục cơ bản được khởi tạo tự động.

## Dữ liệu mẫu

Ứng dụng tự nạp dữ liệu mẫu ở lần chạy đầu nếu cơ sở dữ liệu chưa có giao dịch, ngân sách hoặc lịch định kỳ. Bộ mẫu gồm 5 giao dịch, 1 ngân sách, 1 lịch định kỳ cùng danh mục và ví để người tải về có thể xem ngay tổng quan và thử các chức năng. Nếu cơ sở dữ liệu đã có dữ liệu, ứng dụng sẽ không nạp mẫu đè lên dữ liệu hiện có.

**Lưu ý quyền riêng tư:** Dữ liệu mẫu là bản sao nguyên trạng từ dữ liệu thu chi hiện tại và được đưa vào mã nguồn công khai. Mọi người xem được các số tiền, danh mục, ví và ngày giao dịch trong `Data/sample-data.json`. Người dùng GitHub cài ứng dụng sẽ được nạp một bản riêng vào LocalDB trên máy của họ; dữ liệu không đồng bộ ngược về máy chủ hay database của chủ dự án.

Bộ mẫu có một lịch chi định kỳ đang bật, số tiền **50.000.000 ₫**, kỳ kế tiếp **13/12/2026**. Ứng dụng sẽ tự ghi khoản này khi khởi động sau ngày đến hạn; người thử có thể xóa hoặc điều chỉnh lịch trong mục **Định kỳ**.

## Yêu cầu

- Windows 10/11.
- .NET 8 SDK và SQL Server LocalDB (thường đi kèm Visual Studio).

Mặc định ứng dụng dùng cơ sở dữ liệu `ThuChiCaNhan` trên `(localdb)\MSSQLLocalDB`. Nếu muốn dùng một SQL Server khác, đặt biến môi trường `THUCHI_SQL_CONNECTION` thành connection string phù hợp trước khi mở ứng dụng. Ví dụ:

```powershell
$env:THUCHI_SQL_CONNECTION = "Server=localhost;Database=ThuChiCaNhan;Trusted_Connection=True;TrustServerCertificate=True;"
```

## Chạy ứng dụng

Mở `ThuChiCaNhan.csproj` bằng Visual Studio 2022, hoặc chạy từ thư mục dự án:

```powershell
dotnet restore
dotnet run
```

Cơ sở dữ liệu và bảng sẽ được tạo khi ứng dụng khởi chạy lần đầu. Nếu dùng cơ sở dữ liệu cũ từ bản trước, lúc khởi động ứng dụng sẽ tự bổ sung các bảng ngân sách/ví/định kỳ và gán giao dịch cũ vào ví **Tiền mặt**; dữ liệu giao dịch cũ được giữ nguyên. Gói NuGet sử dụng: EF Core SQL Server, LiveCharts2 và ClosedXML.
