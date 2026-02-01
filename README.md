# 🐳 FoodHub - Restaurant Management System

Dự án **FoodHub** hỗ trợ chạy toàn bộ hệ thống (Database, Backend, Frontend) thông qua Docker Compose.

## 📋 Yêu cầu
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed & running.

## 🚀 Quick Start
Chạy lệnh sau tại thư mục gốc của dự án:

```bash
docker-compose up -d --build
```

---

## 🛠️ Services Information

| Service | Container Name | Port | URL | Description |
| :--- | :--- | :--- | :--- | :--- |
| **Frontend** | `foodhub_frontend` | `3000` | [http://localhost:3000](http://localhost:3000) | Giao diện Next.js cho người dùng cuối |
| **Backend** | `foodhub_backend` | `5000` | [http://localhost:5000/swagger](http://localhost:5000/swagger) | .NET API Server & Swagger Docs |
| **Database** | `foodhub_db` | `5432` | `postgres://localhost:5432` | PostgreSQL Database |

---

## 🔐 Default Accounts (Seeded Data)
Dữ liệu mẫu sẽ tự động được khởi tạo khi chạy lần đầu:

| Role | Username | Password |
| :--- | :--- | :--- |
| 🛡️ **Manager** | `admin` | `admin` |
| 👨‍🍳 **Chef** | `chef` | `chef` |
| 🤵 **Waiter** | `waiter` | `waiter` |
| 💰 **Cashier** | `cashier` | `cashier` |

---

## ❓ Troubleshooting
- **Lỗi kết nối FE <-> BE**: Đảm bảo `NEXT_PUBLIC_API_URL=http://localhost:5000` trong `docker-compose.yml`.
- **Cập nhật code**: Chạy lại lệnh `docker-compose up -d --build` sau khi sửa code.
