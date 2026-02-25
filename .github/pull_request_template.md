## 🏛️ Governance Checklist (FoodHub Guardian v2.1) — Backend

> **Bắt buộc hoàn thành trước khi merge.** Đánh dấu ✅ hoặc ghi N/A nếu không applicable.

---

### 📋 FFA-FLW — Flow Order

- [ ] Pipeline tuân thủ thứ tự: `Validation → Log Start → Auth → Transaction → Logic → Save → Commit → Mapping → Return`
- [ ] Không có business logic nào bị skip hoặc sai thứ tự

### 🔒 FFA-TXG — Transaction Safety

- [ ] Handler có ≥ 2 write operations → đã dùng `IUnitOfWork` với `BeginTransactionAsync`
- [ ] Có `CommitTransactionAsync` sau khi ghi thành công
- [ ] Có `RollbackTransactionAsync` trong khối `catch`
- [ ] External calls (email, API) **không** bị bọc vào transaction

### 📝 FFA-LOG — Logging Compliance

- [ ] Log `Information` ở đầu Handler (request received)
- [ ] Log `Information` ở cuối Handler (success)
- [ ] **KHÔNG** log: password, token, secret, cardNumber, pin, apiKey
- [ ] Dùng Structured Logging: `_logger.LogInformation("Message {Param}", value)` — không concat string

### 🏗️ FFA-CAG — Clean Architecture

- [ ] Handler không gán property Entity trực tiếp (`entity.Status = x` ← sai)
- [ ] State change nằm trong Entity method (`entity.UpdateStatus(x)` ← đúng)
- [ ] Domain layer không import namespace của Application/Infrastructure

### 🎮 FFA-CTL — Thin Controller

- [ ] Controller kế thừa `ApiControllerBase`
- [ ] Inject `IMediator` — không inject Repository hay Service trực tiếp
- [ ] Action method không có if/else logic nghiệp vụ
- [ ] Dùng `HandleResult(result)` cho mọi response

### 📄 FFA-ACV — API Contract

- [ ] Mọi required field có `RuleFor` trong Validator
- [ ] String field có `.MaximumLength(N)`
- [ ] Dùng đúng type: `Guid` cho ID, `Enum` cho status, `decimal` cho tiền
- [ ] XML `<summary>` cho Swagger docs

### 🔐 FFA-SEC — Security

- [ ] Mọi endpoint cần bảo vệ có `[Authorize]` attribute
- [ ] Response DTO không chứa: PasswordHash, Token, SecretKey
- [ ] Endpoint thay đổi state có CSRF protection (nếu dùng Cookie auth)
- [ ] Fine-grained authorization (ownership check) trong Handler nếu cần

### ⚡ FFA-PERF — Performance (cho List/Query endpoints)

- [ ] List query có Pagination (Skip+Take hoặc Cursor)
- [ ] Read-only query dùng `.AsNoTracking()`
- [ ] Không có N+1 pattern (DB call trong vòng lặp)
- [ ] Dùng `.Select()` projection thay vì load toàn bộ Entity

### 🚨 FFA-ERR — Error Handling

- [ ] Dùng **custom exception** (`BusinessException`, `NotFoundException`) — không throw raw `Exception`
- [ ] Handler **không catch tổng quát** rồi swallow — để ExceptionMiddleware xử lý
- [ ] Error response format nhất quán qua `ErrorResponse`

### 🧪 FFA-TST — Test Compliance

- [ ] Command Handler có test file tương ứng (`{HandlerName}Tests.cs`)
- [ ] Test cover **happy path** (success scenario)
- [ ] Test cover **error path** (validation fail, not found, business rule)
- [ ] Mock đúng **interface** (`IUnitOfWork`) — không gọi DB thật

---

### 📊 Self-Assessment Score (FGO)

| Skill    | Weight | Estimate |
| -------- | ------ | -------- |
| FFA-FLW  | 20%    | /100     |
| FFA-TXG  | 15%    | /100     |
| FFA-LOG  | 10%    | /100     |
| FFA-CTL  | 10%    | /100     |
| FFA-CAG  | 15%    | /100     |
| FFA-ACV  | 10%    | /100     |
| FFA-SEC  | 5%     | /100     |
| FFA-PERF | 5%     | /100     |
| FFA-ERR  | 5%     | /100     |
| FFA-TST  | 5%     | /100     |

> ❌ **Block merge** nếu: multi-write không có transaction, Handler gán property Entity trực tiếp, log chứa password/token, throw raw `Exception`, endpoint thiếu `[Authorize]`, Command Handler không có test.
