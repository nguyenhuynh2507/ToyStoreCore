# ToyStoreCore

A full-stack toy e-commerce platform built with **ASP.NET Core 8 MVC** + **EF Core / PostgreSQL**. The project covers a customer-facing online store, a complete admin back office (products, orders, promotions, inventory), an in-store Point-of-Sale (POS) module, and HR management (staff, shifts, payroll) — rebuilt from a legacy version that ran on Oracle.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8 MVC, C# |
| ORM / Database | Entity Framework Core, PostgreSQL (Npgsql) |
| Auth | ASP.NET Core Identity — 3 roles: `Admin`, `Staff`, `Customer` |
| Frontend | Razor Views, vanilla JS, Chart.js (CDN) |
| Session | Cookie-based session for the guest cart |

## Features

### Customer
- Browse & search products by category and price range, with search autocomplete
- Shopping cart works for both guests (session-based) and logged-in users (persisted to the DB, auto-merged on login)
- Select specific items to check out, or "Buy Now" to skip the cart entirely
- **Membership-tier discounts** (4 tiers: Bronze/Silver/Gold/Diamond, auto-upgraded based on completed orders) and **promotion/voucher codes**
- Account page, order history, and self-service cancellation of pending orders
- Related-product recommendations ("You may also like") using an Apriori-based algorithm driven by customer activity
- Multiple images per product (gallery)

### Admin (`/Admin/*`)
- CRUD for products, categories, homepage banners, and promotion codes
- Customer management (search, order history, lock/unlock accounts)
- Order management with status updates (auto-updates membership progress on completion)
- Goods receipt / inventory intake (multi-line forms, auto stock updates, cost totals)
- Customer activity log (feeds the recommendation engine)
- Monthly/date-range revenue and gross-profit reports
- Statistics dashboard (Chart.js): revenue by month/quarter/year, top products and customers

### POS & HR
- In-store checkout screen (`/Admin/Pos`): fast product search, lookup returning customers by phone to auto-apply membership discounts, and instantly complete orders
- Staff management, shift scheduling, and attendance tracking (`/Admin/Staff`, `/Admin/WorkShift`)
- Monthly payroll calculation based on completed shifts (`/Admin/Payroll`)

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL (local or Docker)

## Getting Started

1. Spin up a PostgreSQL database, e.g. with Docker:
   ```bash
   docker run --name toystorecore-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=toystorecore -p 5432:5432 -d postgres:16
   ```
2. Check/update the connection string in `src/ToyStoreCore.Web/appsettings.json` (`ConnectionStrings:DefaultConnection`).
3. Restore and run:
   ```bash
   cd src/ToyStoreCore.Web
   dotnet restore
   dotnet run
   ```
4. The app creates the schema (`EnsureCreated`) and seeds sample data (products, membership tiers, promo codes, admin account) on first run.
5. Open the URL printed in the console (usually `https://localhost:5001`).

### Default admin account (seeded — change before deploying anywhere real)
- Email: `admin@toystorecore.local`
- Password: `Admin@123`

## Project Structure

```
ToyStoreCore.sln
src/ToyStoreCore.Web/
  Controllers/          MVC controllers (Home, Products, Cart, Orders, Account, Admin/*)
  Models/                EF entities (Product, Order, Cart, MembershipTier, Promotion, Hr, ...) + ViewModels
  Data/                  AppDbContext, DbInitializer, IdentitySeeder
  Services/              CartService, PromotionService, MembershipService,
                         ActivityTrackingService, RecommendationService
  Views/                 Razor views per controller
  wwwroot/               Static css/js, uploaded product images
```

## Technical Notes

- `EnsureCreated()` is used for fast schema creation in dev; switch to **EF Core Migrations** when changing models to avoid data loss.
- Product images are uploaded directly and stored under `wwwroot/uploads/products/`.
- No real payment/shipping gateway integration (VNPay, GHN) or SignalR — out of scope for this project.
