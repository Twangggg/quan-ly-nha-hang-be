# Báo Cáo Kiểm Tra Lỗi Logic (Merge Code Gần Nhất)

Sau khi rà soát các thay đổi trong commit gần nhất liên quan tới tính năng tự động sinh `Code` (tuân theo `CodePrefix` của Category và `ItemNumber`), tôi phát hiện ra **3 lỗi logic nghiêm trọng** sau đây:

### 1. Thiếu cập nhật `Code` và `ItemNumber` khi thay đổi danh mục (Đổi `CategoryId`)

- **Tệp bị ảnh hưởng**: `UpdateMenuItemHandler.cs`, `UpdateSetMenuHandler.cs`
- **Mô tả lỗi**: Khi bạn gọi API Update để chuyển một `MenuItem` hoặc `SetMenu` sang một `CategoryId` khác, hiện tại code chỉ cập nhật thuộc tính `CategoryId` nhưng **không sinh lại `Code` và `ItemNumber` mới** cho món ăn.
- **Hậu quả**:
    - Sai logic Format Code: Ví dụ một món đồ uống có mã `DRK-001`, nếu bị chuyển qua danh mục "Món Chính" (CodePrefix là `MAIN`), nó vẫn giữ nguyên mã `DRK-001` cũ.
    - **Lỗi Database (Sập API)**: Trong DB có constraint `Unique(CategoryId, ItemNumber)`. Nếu danh mục mới vô tình đã có một món khác mang `ItemNumber` trùng với `ItemNumber` cũ của món đang được chuyển, Entity Framework sẽ throw ra lỗi **Unique Constraint Violation (HTTP 500)** thay vì xử lý êm đẹp.

### 2. Cho phép Update `CategoryType` tự do khi Category đã có Item

- **Tệp bị ảnh hưởng**: `UpdateCategoryHandler.cs`
- **Mô tả lỗi**: Bạn đang cho phép thay đổi tuỳ ý `CategoryType` (từ `Normal` sang `Combo` và ngược lại) mà không hề check xem Category đó có đang chứa `MenuItem` hay `SetMenu` nào bên trong chưa.
- **Hậu quả**: Giả sử Category "Món Chính" (Normal) đang chứa 10 `MenuItem`. Admin đổi nó thành `Combo`. Lúc này 10 `MenuItem` sẽ bị lỗi mỗi khi Admin gọi API `UpdateMenuItem` trên chúng (vì trong hàm `UpdateMenuItemHandler` có kiểm tra chặn `if (category.CategoryType != CategoryType.Normal) return Failure;`).

### 3. Vấn đề Race Condition (Xung đột tài nguyên) khi tạo mới

- **Tệp bị ảnh hưởng**: `CreateMenuItemHandler.cs`, `CreateSetMenuHandler.cs`
- **Mô tả lỗi**: Logic lấy `ItemNumber` mới nhất đang dùng `MaxAsync()`:
  `var next = await repo.MaxAsync(m => m.ItemNumber); next++;`
- **Hậu quả**: Nếu có 2 Admin **cùng lúc** ấn tạo món ăn mới cho chung 1 Category, `MaxAsync()` ở cả 2 thread sẽ trả về cùng một giá trị, dẫn tới việc tự động sinh ra mã (VD: `DRK-008`) giống hệt nhau. Khi save DB, request sau sẽ bị crash văng lỗi 500 cho người dùng (do Unique Constraint chặn lại) thay vì xử lý mượt mà. Đáng lẽ đoạn này có thể cải thiện bằng lock, retry, hoặc dùng sequence.

---

**Đề xuất hướng fix:**

1. Trong hàm Update (MenuItem/SetMenu): Nếu `request.CategoryId != menuItem.CategoryId`, phải tính toán lại `ItemNumber` và gán lại `Code` mới cho entity đó.
2. Trong hàm `UpdateCategoryHandler`: `if (await menuRepo.AnyAsync(...) || setMenuRepo.AnyAsync(...))`, chặn không cho đổi `CategoryType`.
3. Có thể tạm bỏ qua (3) nếu ứng dụng quản lý nhà hàng chạy nội bộ, lượng concurrent không đủ cao để gây lỗi liên tục, nhưng nếu muốn cẩn thận thì cần thiết lập Retry Strategy (EF Core Resiliency).
