# ToyStoreCore

Nền tảng thương mại điện tử bán đồ chơi full-stack, xây dựng bằng **ASP.NET Core 8 MVC** + **EF Core / PostgreSQL**. Dự án bao gồm cửa hàng trực tuyến cho khách hàng, hệ thống quản trị đầy đủ (sản phẩm, đơn hàng, khuyến mãi, kho), module bán hàng tại quầy (POS) và quản lý nhân sự (HR/chấm công/lương) — được rebuild lại từ một bản cũ dùng Oracle.

## Tech Stack

| Layer | Công nghệ |
|---|---|
| Backend | ASP.NET Core 8 MVC, C# |
| ORM / Database | Entity Framework Core, PostgreSQL (Npgsql) |
| Auth | ASP.NET Core Identity — 3 role: `Admin`, `Staff`, `Customer` |
| Frontend | Razor Views, vanilla JS, Chart.js (CDN) |
| Session | Cookie-based session cho giỏ hàng khách vãng lai |

## Tính năng

### Khách hàng
- Duyệt & tìm kiếm sản phẩm theo danh mục, khoảng giá, gợi ý tìm kiếm (autocomplete)
- Giỏ hàng: hoạt động cho cả khách vãng lai (session) và khách đăng nhập (lưu DB, tự gộp khi đăng nhập)
- Chọn từng sản phẩm để thanh toán, hoặc "Mua ngay" bỏ qua giỏ hàng
- Áp dụng **giảm giá theo hạng thành viên** (4 hạng: Đồng/Bạc/Vàng/Kim Cương, tự thăng hạng theo số đơn hoàn tất) và **mã khuyến mãi/voucher**
- Trang tài khoản, lịch sử & huỷ đơn hàng đang chờ xử lý
- Gợi ý sản phẩm liên quan ("Có thể bạn cũng thích") bằng thuật toán dạng Apriori dựa trên hành vi khách hàng
- Nhiều ảnh cho một sản phẩm (gallery)

### Quản trị (`/Admin/*`)
- CRUD sản phẩm, danh mục, banner trang chủ, mã khuyến mãi
- Quản lý khách hàng (tìm kiếm, xem lịch sử đơn, khoá/mở khoá tài khoản)
- Quản lý đơn hàng, cập nhật trạng thái (tự động cộng tiến độ hạng thành viên khi hoàn tất)
- Nhập kho (phiếu nhập nhiều dòng, tự cộng tồn kho, tính tổng chi phí)
- Nhật ký hoạt động khách hàng (đầu vào cho hệ gợi ý sản phẩm)
- Báo cáo doanh thu & lợi nhuận gộp theo tháng/khoảng ngày
- Dashboard thống kê (Chart.js): doanh thu theo tháng/quý/năm, top sản phẩm/khách hàng

### POS & HR
- Màn hình bán hàng tại quầy (`/Admin/Pos`): tìm sản phẩm nhanh, tra cứu khách quen theo SĐT, áp giảm giá hạng thành viên tự động, tạo đơn hoàn tất ngay
- Quản lý nhân viên, phân ca làm việc, chấm công (`/Admin/Staff`, `/Admin/WorkShift`)
- Chốt lương theo tháng dựa trên số ca đã hoàn thành (`/Admin/Payroll`)

## Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL (local hoặc Docker)

## Cài đặt & chạy

1. Tạo database Postgres, ví dụ bằng Docker:
   ```bash
   docker run --name toystorecore-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=toystorecore -p 5432:5432 -d postgres:16
   ```
2. Kiểm tra/sửa connection string trong `src/ToyStoreCore.Web/appsettings.json` (`ConnectionStrings:DefaultConnection`).
3. Restore & chạy:
   ```bash
   cd src/ToyStoreCore.Web
   dotnet restore
   dotnet run
   ```
4. Ứng dụng tự tạo schema (`EnsureCreated`) và seed dữ liệu mẫu (sản phẩm, hạng thành viên, mã khuyến mãi, tài khoản admin) khi chạy lần đầu.
5. Mở URL được in ra ở console (thường `https://localhost:5001`).

### Tài khoản admin mặc định (seed sẵn — đổi ngay nếu deploy thật)
- Email: `admin@toystorecore.local`
- Mật khẩu: `Admin@123`

## Cấu trúc thư mục

```
ToyStoreCore.sln
src/ToyStoreCore.Web/
  Controllers/          MVC controllers (Home, Products, Cart, Orders, Account, Admin/*)
  Models/                EF entities (Product, Order, Cart, MembershipTier, Promotion, Hr, ...) + ViewModels
  Data/                  AppDbContext, DbInitializer, IdentitySeeder
  Services/              CartService, PromotionService, MembershipService,
                         ActivityTrackingService, RecommendationService
  Views/                 Razor views theo từng controller
  wwwroot/               css/js tĩnh, ảnh upload sản phẩm
```

## Ghi chú kỹ thuật

- `EnsureCreated()` được dùng để tạo schema nhanh cho môi trường dev; khi thay đổi model nên chuyển sang **EF Core Migrations** để tránh mất dữ liệu.
- Ảnh sản phẩm được upload trực tiếp và lưu tại `wwwroot/uploads/products/`.
- Chưa tích hợp cổng thanh toán/vận chuyển thật (VNPay, GHN) hay SignalR — nằm ngoài phạm vi dự án hiện tại.
