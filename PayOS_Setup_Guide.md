# Hướng Dẫn Tích Hợp Thanh Toán PayOS (VietQR)

---

## 1. Cài đặt tài khoản ngân hàng
Để test tự động, bạn cần tự gắn tài khoản ngân hàng cá nhân của chính mình vào hệ thống phân lập của bạn.
1. Truy cập [https://my.payos.vn/](https://my.payos.vn/) và tạo tài khoản.
2. Tạo một **Kênh thanh toán**, liên kết với ngân hàng cá nhân của bạn.
3. Vào phần **Cài đặt** -> mục **Thông tin kết nối**, lấy 3 đoạn mã: `Client ID`, `API Key`, `Checksum Key`.
4. Mở file `appsettings.Development.json` trong thư mục `FoodHub.WebAPI` của source code Backend và thay thế các mã trên:
   ```json
   "PayOS": {
       "ClientId": "<Client ID của bạn>",
       "ApiKey": "<API Key của bạn>",
       "ChecksumKey": "<Checksum Key của bạn>",
       "ReturnUrl": "http://localhost:3000/order/success",
       "CancelUrl": "http://localhost:3000/order"
   }
   ```

## 2. Cài đặt Ngrok để nhận Webhook
Nếu sử dụng backend ở localHost thì cần tải
sử dụng trên web đã deploy thì không cần
1. Cài đặt ứng dụng Ngrok từ trang chủ.
2. Thêm Authtoken của bạn (lấy trên web Ngrok) bằng lệnh Terminal: `ngrok config add-authtoken <token>`.
3. Bật Backend dự án chạy lên (`dotnet run` - mặc định port `5133`).
4. Mở cửa sổ Terminal mới và chạy: `ngrok http 5133`.
5. Copy đường link bắt đầu bằng `https` từ kết quả của Ngrok (VD: `https://abcd.ngrok-free.app`).
6. Quay lại web PayOS -> tab **Cấu hình Webhook** -> Dán link sau: `https://abcd.ngrok-free.app/api/v1/billing/payos-webhook` -> Bấm nút **Lưu cấu hình**.
*(Cứ mỗi lần bạn tắt bật ngrok thì sẽ có link mới, nhớ chỉnh lại trên web PayOS).*

---

## 3. Cấu hình tham khảo cho FE

### Bước 3.1. Lấy thông tin thanh toán và vẽ mã QR lên màn hình
- **API Call:** `POST /api/v1/billing/orders/{orderId}/payos-qr` (Kèm Token đăng nhập)
- **Response:**
  ```json
  {
    "qrCode": "000201010212385...",
    "bin": "970422",
    "accountNumber": "123456789",
    "accountName": "NGUYEN VAN A",
    "amount": 130000,
    "description": "Thanh toan don 1",
    "currency": "VND"
  }
  ```
- **Xử lý UI:** 
  - Đẩy thẳng biến `qrCode` vào thư viện tạo QR của JavaScript (như `qrcode.react`). Trình duyệt sẽ rọi ra bức ảnh QR chuẩn VietQR cho màn hình thanh toán.
  - Các biến còn lại (`amount`, `accountName`, `accountNumber`...) dùng để hiển thị text bên cạnh mã QR.

### Bước 3.2. Lắng nghe trạng thái hoàn tất (SignalR Real-time)
Để nhảy tab ngay khi quét mã thành công, Frontend **KHÔNG CẦN gọi API liên tục kiểm tra**. Backend đã setup Hub SignalR để nhận luồng tự động.

**Đoạn code kết nối mẫu dành cho màn hình Thanh toán:**
```javascript
import * as signalR from "@microsoft/signalr";

// 1. Kết nối thẳng vào kênh Billing của Backend
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5133/hubs/billing")
    .withAutomaticReconnect()
    .build();

// 2. Bắt đài (Lắng nghe) sự kiện "OrderStatusChanged" từ Backend
connection.on("OrderStatusChanged", (data) => {
    // data payload do BE đẩy về: { orderId: "guid", status: "Paid" }
    if (data.orderId === currentOrderId && data.status === "Paid") { // Trùng đơn hiện tại
        alert("Khách đã chuyển tiền! Giao dịch thành công.");
        // Chèn code điều hướng trang, xóa giỏ hàng tại đây (VD: router.push('/success'))
    }
});

// 3. Khởi động SignalR
connection.start().catch(err => console.error("Lỗi SignalR: ", err));
```
