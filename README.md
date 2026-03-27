# 🐳 FoodHub Backend - Restaurant Management System API

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-blue.svg)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7.2-red.svg)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

FoodHub Backend là API server cho hệ thống quản lý nhà hàng FoodHub, được xây dựng bằng ASP.NET Core với kiến trúc Clean Architecture. Dự án hỗ trợ chạy toàn bộ hệ thống (Database, Backend, Frontend) thông qua Docker Compose.

## 📋 Mục lục

- [Tổng quan](#-tổng-quan)
- [Tính năng](#-tính-năng)
- [Kiến trúc](#-kiến-trúc)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Yêu cầu hệ thống](#-yêu-cầu-hệ-thống)
- [Cài đặt](#-cài-đặt)
- [Chạy ứng dụng](#-chạy-ứng-dụng)
- [Tài liệu API](#-tài-liệu-api)
- [Kiểm thử](#-kiểm-thử)
- [Đóng góp](#-đóng-góp)
- [Giấy phép](#-giấy-phép)

## 🌟 Tổng quan

FoodHub Backend cung cấp RESTful API cho hệ thống quản lý nhà hàng, bao gồm quản lý nhân viên, thực đơn, đơn hàng, và các chức năng liên quan. API được thiết kế theo chuẩn REST với versioning, authentication JWT, và documentation tự động qua Swagger.

## ✨ Tính năng

- **Quản lý nhân viên**: CRUD operations cho nhân viên với vai trò khác nhau (Manager, Chef, Cashier)
- **Quản lý thực đơn**: Categories, Menu Items, Set Menus với tùy chọn linh hoạt
- **Quản lý đơn hàng**: Tạo và theo dõi đơn hàng với trạng thái real-time
- **Authentication & Authorization**: JWT-based authentication với refresh tokens
- **Caching**: Redis cache cho performance tối ưu
- **Email notifications**: Background jobs cho gửi email
- **Media management**: Cloudinary integration cho upload hình ảnh
- **Localization**: Hỗ trợ đa ngôn ngữ (Tiếng Việt, Tiếng Anh)
- **API Versioning**: Versioning cho API compatibility
- **Rate Limiting**: Bảo vệ API khỏi abuse (Global & Endpoint level)
- **Security**: Chống tấn công CSRF (Double Submit Cookie) và XSS (HttpOnly)
- **Health Checks**: Tự động giám sát trạng thái DB & Redis qua endpoint `/health`
- **Observability**: Tích hợp OpenTelemetry cho Tracing và Metrics

## 🏗️ Kiến trúc

Dự án sử dụng **Clean Architecture** với 4 layers chính:

```
FoodHub.WebAPI (Presentation Layer)
    ├── Controllers
    ├── Middleware
    └── Extensions

FoodHub.Application (Application Layer)
    ├── Features (CQRS pattern)
    ├── Services
    ├── Validators
    └── Interfaces

FoodHub.Domain (Domain Layer)
    ├── Entities
    ├── Enums
    └── Value Objects

FoodHub.Infrastructure (Infrastructure Layer)
    ├── Persistence (EF Core, Repositories)
    ├── Services (Email, Cloudinary, etc.)
    └── Security (JWT, Password hashing)
```

## 🛠️ Công nghệ sử dụng

### Backend Framework

- **ASP.NET Core 9.0** - Web API framework
- **Entity Framework Core 9.0** - ORM cho database operations
- **MediatR** - CQRS pattern implementation
- **FluentValidation** - Request validation

### Database & Caching

- **PostgreSQL 15** - Primary database
- **Redis 7.2** - Caching và session storage

### Authentication & Security

- **JWT Bearer Authentication** - Token-based auth
- **BCrypt** - Password hashing
- **Rate Limiting** - API protection (Global Limiter)
- **Anti-CSRF** - Double Submit Cookie protection

### External Services

- **Cloudinary** - Media storage và optimization
- **SMTP** - Email sending (Gmail SMTP)

### Development Tools

- **Swagger/OpenAPI** - API documentation
- **Docker & Docker Compose** - Containerization
- **Serilog** - Structured logging (Console & File)
- **OpenTelemetry** - Tracing & Metrics (Observability)
- **Health Checks** - System diagnostics for PostgreSQL & Redis
- **xUnit** - Unit testing
- **AutoMapper** - Object mapping

### Additional Features

- **API Versioning** - Version management
- **Response Compression** - Gzip/Brotli compression
- **CORS** - Cross-origin resource sharing
- **Localization** - Multi-language support
- **Background Jobs** - Email processing

## 📋 Yêu cầu hệ thống

### Development Environment

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [PostgreSQL](https://www.postgresql.org/) (hoặc sử dụng Docker)
- [Redis](https://redis.io/) (hoặc sử dụng Docker)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) hoặc [VS Code](https://code.visualstudio.com/)

### Production Environment

- Docker runtime
- PostgreSQL database
- Redis cache
- SMTP server (Gmail recommended)

## 🚀 Cài đặt

### 1. Clone repository

```bash
git clone <repository-url>
cd FoodHub_BE
```

### 2. Environment Variables

Tạo file `.env` trong thư mục `FoodHub.WebAPI`:

```env
# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=FoodHub
DB_USER=postgres
DB_PASSWORD=your_password

# JWT
JWT_SECRET_KEY=your_super_secret_key_min_32_chars
JWT_ISSUER=FoodHub_Server
JWT_AUDIENCE=FoodHub_Client
JWT_ACCESS_TOKEN_EXPIRES_IN_MINUTE=60
JWT_REFRESH_TOKEN_EXPIRES_IN_DAYS=7

# Redis
REDIS_CONNECTION=localhost:6379

# Email
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_SENDER_EMAIL=your_email@gmail.com
EMAIL_SENDER_NAME=FoodHub
EMAIL_APP_PASSWORD=your_app_password

# Cloudinary (optional)
CLOUDINARY_CLOUD_NAME=your_cloud_name
CLOUDINARY_API_KEY=your_api_key
CLOUDINARY_API_SECRET=your_api_secret

# PayOS
PAYOS_CLIENT_ID=your_payos_client_id
PAYOS_API_KEY=your_payos_api_key
PAYOS_CHECKSUM_KEY=your_payos_checksum_key
PAYOS_RETURN_URL=http://localhost:3000/order/success
PAYOS_CANCEL_URL=http://localhost:3000/order

# CORS
ALLOWED_ORIGINS=http://localhost:3000,http://localhost:3001
```

### 3. Database Setup

```bash
# Sử dụng Docker
docker run --name foodhub-postgres -e POSTGRES_PASSWORD=123456@ -e POSTGRES_DB=FoodHub -p 5432:5432 -d postgres:alpine

# Hoặc cài đặt PostgreSQL locally
```

### 4. Redis Setup

```bash
# Sử dụng Docker
docker run --name foodhub-redis -p 6379:6379 -d redis:alpine

# Hoặc cài đặt Redis locally
```

## 🏃‍♂️ Chạy ứng dụng

### Development Mode (Local)

```bash
# Restore packages
dotnet restore

# Run migrations
dotnet ef database update --project FoodHub.WebAPI

# Run application
dotnet run --project FoodHub.WebAPI
```

Ứng dụng sẽ chạy tại: http://localhost:5000

### Production Mode (Docker)

```bash
# Từ thư mục gốc của dự án
docker-compose up -d --build
```

## 📚 Tài liệu API

### Swagger Documentation

Khi ứng dụng đang chạy, truy cập:

- **Swagger UI**: http://localhost:5000/swagger
- **API Version v1.0**: http://localhost:5000/swagger/v1.0/swagger.json

### Health Check Endpoints

- **Simple Health Check**: http://localhost:5000/health
- **Detailed Health Check (JSON)**: http://localhost:5000/health/detail

### API Endpoints

- `POST /api/v1/auth/login` - Đăng nhập
- `GET /api/v1/employees` - Lấy danh sách nhân viên
- `GET /api/v1/menu-items` - Lấy danh sách món ăn
- `POST /api/v1/orders` - Tạo đơn hàng
- Và nhiều endpoints khác...

### Authentication

Sử dụng JWT token trong header:

```
Authorization: Bearer <your_jwt_token>
```

## 🧪 Kiểm thử

### Unit Tests

```bash
dotnet test FoodHub.Tests
```

### Integration Tests

```bash
# Chạy với database test
dotnet test FoodHub.Tests --filter Category=Integration
```

### Code Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./coverage/lcov.info
```

## 🤝 Đóng góp

1. Fork project
2. Tạo feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Tạo Pull Request

### Coding Standards

- Sử dụng C# coding conventions
- Viết unit tests cho logic phức tạp
- Update documentation khi cần thiết
- Sử dụng meaningful commit messages

## 📄 Giấy phép

Dự án này được phân phối dưới giấy phép MIT. Xem file `LICENSE` để biết thêm chi tiết.

## 📞 Liên hệ

- **Project**: FoodHub Restaurant Management System
- **Email**: foodhubojt.noreply@gmail.com
- **Documentation**: [FoodHub Docs](../FoodHub_Docs/)

---

## 🛠️ Services Information

| Service      | Container Name     | Port   | URL                                                            | Description                           |
| :----------- | :----------------- | :----- | :------------------------------------------------------------- | :------------------------------------ |
| **Frontend** | `foodhub_frontend` | `3000` | [http://localhost:3000](http://localhost:3000)                 | Giao diện Next.js cho người dùng cuối |
| **Backend**  | `foodhub_backend`  | `5000` | [http://localhost:5000/swagger](http://localhost:5000/swagger) | .NET API Server & Swagger Docs        |
| **Database** | `foodhub_db`       | `5432` | `postgres://localhost:5432`                                    | PostgreSQL Database                   |
| **Cache**    | `foodhub_redis`    | `6379` | `redis://localhost:6379`                                       | Redis Cache Server                    |

## 🔐 Default Accounts (Seeded Data)

Dữ liệu mẫu sẽ tự động được khởi tạo khi chạy lần đầu:

| 🛡️ **Manager** | `admin`   | `New123!!` |
| 👨‍🍳 **Chef**    | `chef`    | `New123!!` |
| 💰 **Cashier** | `cashier` | `New123!!` |

## ❓ Troubleshooting

### Lỗi kết nối Database

- Đảm bảo PostgreSQL đang chạy và connection string đúng
- Kiểm tra firewall settings

### Lỗi JWT Authentication

- Đảm bảo `JWT_SECRET_KEY` có ít nhất 32 ký tự
- Kiểm tra `JWT_ISSUER` và `JWT_AUDIENCE` khớp nhau

### Lỗi kết nối FE <-> BE

- Đảm bảo `NEXT_PUBLIC_API_URL=http://localhost:5000` trong `docker-compose.yml`
- Kiểm tra CORS settings

### Lỗi Email

- Đảm bảo Gmail App Password đúng
- Kiểm tra SMTP settings

### Cập nhật code

- Chạy lại lệnh `docker-compose up -d --build` sau khi sửa code
- Clear browser cache nếu cần

### Performance Issues

- Kiểm tra Redis connection
- Monitor database queries
- Check rate limiting settings
