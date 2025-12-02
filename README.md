# Fashion Store - Dự án cá nhân
# Giới thiệu
Đây là một website thương mại điện tử xây dựng bằng C# / ASP.NET Core MVC. Người dùng có thể xem sản phẩm, thêm vào giỏ hàng, đặt mua và theo dõi đơn hàng một cách dễ dàng. Hệ thống còn cung cấp trang quản trị giúp Admin quản lý sản phẩm, danh mục, người dùng và đơn hàng hiệu quả.
# Tính năng chính
<ul>
  <li><strong>Quản lý sản phẩm (CRUD)</strong>: Thêm, sửa, xóa và xem thông tin sản phẩm; quản lý hình ảnh, giá và mô tả.</li>
  <li><strong>Quản lý danh mục sản phẩm</strong>: Tạo, sửa, xóa các danh mục để phân loại sản phẩm, giúp người dùng tìm kiếm dễ dàng.</li>
  <li><strong>Quản lý giỏ hàng</strong>: Người dùng có thể thêm, xóa, điều chỉnh số lượng sản phẩm trong giỏ hàng trước khi đặt mua.</li>
  <li><strong>Đặt hàng và thanh toán</strong>: Người dùng đặt đơn, chọn phương thức thanh toán và hệ thống cập nhật trạng thái đơn hàng.</li>
  <li><strong>Theo dõi trạng thái đơn hàng</strong>: Khách hàng và Admin có thể xem trạng thái hiện tại của từng đơn hàng (chưa xử lý, đang giao, đã giao, hủy).</li>
  <li><strong>Chat giữa người mua và chủ shop</strong>: Giao tiếp trực tiếp để hỏi đáp về sản phẩm hoặc hỗ trợ đơn hàng.</li>
  <li><strong>Thông báo qua email</strong>: Gửi thông báo khi có đơn hàng mới, trạng thái đơn thay đổi hoặc chương trình khuyến mãi.</li>
  <li><strong>Áp dụng các chương trình giảm giá</strong>: Thiết lập mã giảm giá hoặc ưu đãi theo sản phẩm, danh mục hoặc toàn bộ đơn hàng.</li>
  <li><strong>Quản lý bình luận sản phẩm</strong>: Người dùng có thể bình luận, đánh giá sản phẩm; Admin quản lý, duyệt hoặc xóa bình luận.</li>
</ul>

# Cộng nghệ sử dụng
<ul>
  <li><strong>Backend:</strong> ASP.NET Core 9.0 (MVC thuần)</li>
  <li><strong>Database:</strong> SQL Server</li>
  <li><strong>ORM:</strong> Entity Framework Core</li>
  <li><strong>Authentication:</strong> RBAC với ASP.NET Core Identity</li>
  <li><strong>Realtime Chat:</strong> SignalR</li>
  <li><strong>Cloud Storage:</strong> Cloudinary (Lưu trữ ảnh sản phẩm)</li>
  <li><strong>Thanh toán:</strong> VNPay</li>
  <li><strong>Email:</strong> SMTP</li>
  <li><strong>Documentation:</strong> Swagger</li>
  <li><strong>Deployment:</strong> Local</li>
</ul>

 # Cấu trúc dự án
```text
ProjectTest1/
├── Controllers/       # Xử lý luồng chính (Logic điều hướng)
├── Models/            # Cấu trúc dữ liệu (Database Entities)
├── Repository/        # Lớp giao tiếp dữ liệu (Data Access)
├── Views/             # Giao diện người dùng (UI)
├── ViewModels/        # Dữ liệu chuyển đổi cho View
├── Hubs/              # Xử lý thời gian thực (SignalR)
├── Migrations/        # Lịch sử thay đổi Database
├── Helpper/           # Các hàm tiện ích
├── wwwroot/           # File tĩnh (CSS, JS, Ảnh)
├── appsettings.json   # Cấu hình hệ thống
└── Program.cs         # Khởi chạy ứng dụng
 ```
# Cài đặt và chạy dự án
## Yêu cầu hệ thống
<ul>
  <li><strong>.NET 9.0 SDK</strong></li>
  <li><strong>SQL Server 2022 (hoặc bất kỳ cơ sở dữ liệu nào hỗ trợ EF Core)</strong></li>
  <li><<strong>Visual Studio 2022 hoặc bất kỳ IDE nào hỗ trợ .NET</strong></li>
</ul>
    
## Các bước cài đặt
1. **Clone repository:**
```text
git clone https://github.com/levinh369/dotnet-ecommerce.git
cd dotnet-ecommerce
```
2. **Khôi phục các packages:**
```text
dotnet restore
```
3. **Đổi tên appsettings.template thành appsettings.json và cấu hình các thông tin sau trong appsettings.json:**
```text
 {
  "ConnectionStrings": {
    "MyDB": "Server=localhost;Database=DoAn;User Id=sa;Password=XXX;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Cloudinary": {
    "CloudName": "XXX",
    "ApiKey": "XXX",
    "ApiSecret": "XXX"
  },
  "Vnpay": {
    "TmnCode": "XXX",
    "HashSecret": "XXX",
    "BaseUrl": "[https://sandbox.vnpayment.vn/paymentv2/vpcpay.html](https://sandbox.vnpayment.vn/paymentv2/vpcpay.html)",
    "CallbackUrl": "https://localhost:44355/Payment/Callback",
    "Version": "2.1.0",
    "Command": "pay",
    "CurrCode": "VND",
    "Locale": "vn",
    "TimeZoneId": "SE Asia Standard Time",
    "BankCode": "NCB"
  },
  "EmailSettings": {
    "From": "XXX@gmail.com",
    "SenderName": "Fashion Store",
    "Password": "XXX",
    "Host": "smtp.gmail.com",
    "Port": 587
  }
}
```
Có thể tham khảo schema của database tại file data.sql

4. **Chạy ứng dụng:**
```text
dotnet run
```
