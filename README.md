> **Live site:** https://practora.store  
> Built end-to-end by **Mohamed Makawye** — Full-Stack .NET Developer ([GitHub](https://github.com/teva-12)).  
> This repository is a sanitized showcase of the production code: no secrets, no real data, no history.  
> All rights reserved — the code is published for viewing and portfolio purposes only.

<!-- TODO: add screenshots here (home, product, customize page, admin dashboard) -->

# ✦ Practora — Premium E-Commerce Platform

A production-ready, full-stack ASP.NET Core 9 e-commerce application built with Clean Architecture, supporting physical & digital products, full order lifecycle, admin dashboard with analytics, and an elegant corporate UI with dark mode.

---

## 🚀 Tech Stack

| Layer | Technology |
|---|---|
| **Framework** | ASP.NET Core 9 MVC |
| **Architecture** | Clean Architecture + Repository Pattern |
| **Database** | SQL Server (EF Core Code-First) |
| **Authentication** | ASP.NET Core Identity + JWT-ready |
| **Logging** | Serilog |
| **Frontend** | Vanilla CSS/JS (no framework dependency) |
| **Charts** | Chart.js (admin dashboard) |

---

## 📁 Project Structure

```
Practora/
├── src/
│   ├── Practora.Core/             # Domain layer
│   │   ├── Entities/                  # Domain entities
│   │   ├── DTOs/                      # Data transfer objects
│   │   ├── Interfaces/
│   │   │   ├── Repositories/          # Repository contracts
│   │   │   └── Services/              # Service contracts
│   │   └── Enums/                     # Domain enumerations
│   │
│   ├── Practora.Infrastructure/   # Data access layer
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/        # EF Core entity configs
│   │   │   └── Seeders/               # Database seeder
│   │   ├── Repositories/              # Repository implementations
│   │   └── Services/                  # Service implementations
│   │
│   └── Practora.Web/              # Presentation layer
│       ├── Areas/Admin/               # Admin area (dashboard, products, orders)
│       ├── Controllers/               # MVC controllers
│       ├── Views/                     # Razor views
│       ├── ViewModels/                # View-specific models
│       ├── Middleware/                # Custom middleware
│       └── wwwroot/                   # Static assets (CSS, JS)
│
├── Database/
│   └── Scripts/                       # SQL scripts
│
├── Practora.sln
└── README.md
```

---

## ⚡ Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or SQL Server Express / LocalDB
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code

### 1. Clone the repository

```bash
git clone https://github.com/yourusername/Practora.git
cd Practora
```

### 2. Configure the database connection

Edit `src/Practora.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PractoraDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

For SQL Server Express:
```
Server=.\\SQLEXPRESS;Database=PractoraDb;Trusted_Connection=True;TrustServerCertificate=True
```

### 3. Run EF Core Migrations

```bash
cd src/Practora.Web
dotnet ef migrations add InitialCreate --project ../Practora.Infrastructure
dotnet ef database update
```

### 4. Run the application

```bash
dotnet run
```

Or open `Practora.sln` in Visual Studio and press **F5**.

The application will automatically:
- Create the database schema
- Run all migrations
- Seed categories, products, coupons, and the admin user

### 5. Access the application

| URL | Description |
|---|---|
| `https://localhost:7000` | Storefront |
| `https://localhost:7000/Admin` | Admin Dashboard |

**Default Admin Credentials:**
- Email: `admin@shopelegance.com`
- Password: `Admin@123456!`

---

## 🎯 Features

### Storefront
- ✅ Responsive design (mobile, tablet, desktop)
- ✅ Dark / light mode toggle
- ✅ Product browsing with filters (category, price, type, availability)
- ✅ Product search & sorting
- ✅ Product detail with image gallery, tabs, and reviews
- ✅ Shopping cart (AJAX, persistent, session-based for guests)
- ✅ Wishlist (authenticated users)
- ✅ Coupon / discount codes
- ✅ Secure checkout flow
- ✅ Order confirmation page
- ✅ Customer account portal (profile, orders, change password)

### Admin Panel
- ✅ Dashboard with KPI cards (revenue, orders, customers, products)
- ✅ Revenue chart (monthly, Chart.js)
- ✅ Orders by status doughnut chart
- ✅ Recent orders & top products tables
- ✅ Full product CRUD (images, tags, SEO fields, pricing)
- ✅ Order management with status workflow & history timeline
- ✅ Low-stock alerts
- ✅ Role-based access (Admin / Manager)

### Security
- ✅ ASP.NET Core Identity with strong password policy
- ✅ CSRF protection (AntiForgery tokens)
- ✅ XSS protection headers
- ✅ Clickjacking prevention (X-Frame-Options)
- ✅ SQL injection prevention (EF Core parameterized queries)
- ✅ Soft-delete (data is never hard-deleted)
- ✅ Account lockout after 5 failed attempts

---

## 🗄️ Database

| Entity | Description |
|---|---|
| `Products` | Physical & digital products with images and tags |
| `Categories` | Hierarchical categories |
| `Orders` | Full order lifecycle with status history |
| `OrderItems` | Denormalized for historical accuracy |
| `Carts` | Persistent carts (user + session) |
| `Wishlists` | Per-user wishlists |
| `Reviews` | Moderated product reviews |
| `Coupons` | Percentage & fixed-amount coupons |
| `Addresses` | Saved user addresses |

---

## 🔑 Default Seed Data

- **Admin account:** `admin@shopelegance.com` / `Admin@123456!`
- **6 categories:** Electronics, Fashion, Home & Living, Books, Digital Downloads, Sports
- **8 sample products** (mix of physical, digital, both)
- **3 coupons:** `WELCOME10`, `SAVE20`, `SUMMER25`

---

## 🧩 Extending the Application

### Add Stripe Payments
1. Install `Stripe.net` NuGet package
2. Add keys to `appsettings.json` under `Stripe`
3. Implement `IPaymentService` in Infrastructure
4. Update `CheckoutController.PlaceOrder`

### Add Email Notifications
1. Install `MailKit` or `SendGrid`
2. Implement `IEmailService`
3. Register in `DependencyInjection.cs`

---

## 📜 License

MIT License — see [LICENSE](LICENSE) for details.

---

## 🤝 Contributing

Pull requests are welcome. For major changes, please open an issue first.
