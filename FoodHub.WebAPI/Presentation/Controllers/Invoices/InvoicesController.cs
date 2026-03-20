using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Invoices.Commands.CreateInvoice;
using FoodHub.Application.Features.Invoices.Queries.GetInvoiceById;
using FoodHub.Application.Features.Invoices.Queries.GetInvoicePdf;
using FoodHub.Application.Features.Invoices.Queries.GetInvoices;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Invoices
{
    /// <summary>
    /// Controller quản lý các hoạt động liên quan đến hóa đơn (invoices) trong hệ thống.
    /// Controller này cung cấp các endpoint để người dùng có thể xem danh sách hóa đơn, tải xuống hóa đơn dưới dạng tệp PDF, và tạo hóa đơn mới cho các đơn hàng đã hoàn thành.
    /// Các endpoint trong controller này được bảo vệ bằng các quyền truy cập cụ thể để đảm bảo rằng chỉ những người dùng có quyền mới có thể thực hiện các hành động liên quan đến hóa đơn.
    /// Controller này sử dụng MediatR để xử lý các lệnh và truy vấn liên quan đến hóa đơn, giúp tách biệt logic nghiệp vụ khỏi lớp trình bày và cải thiện khả năng bảo trì của mã nguồn.
    /// </summary>
    [Tags("Hóa đơn (Invoices)")]
    public class InvoicesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InvoicesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Xem danh sách hóa đơn với phân trang và lọc.
        /// Cho phép người dùng xem các hóa đơn đã tạo, chi tiết từng hóa đơn, và trạng thái của chúng.
        /// </summary>
        /// <returns>
        /// Trả về danh sách hóa đơn đã tạo, chi tiết từng hóa đơn, và trạng thái của chúng.
        /// Endpoint này sẽ trả về một danh sách các hóa đơn đã được tạo trong hệ thống, cùng với chi tiết của từng hóa đơn như số hóa đơn, ngày tạo, tên nhân viên thu ngân, số bàn, phương thức thanh toán và tổng số tiền.
        /// Người dùng có thể sử dụng endpoint này để xem lại các hóa đơn đã tạo, kiểm tra chi tiết của từng hóa đơn và theo dõi trạng thái của chúng (ví dụ: đã thanh toán, đang xử lý, v.v.).
        /// Endpoint cũng hỗ trợ phân trang và lọc để người dùng có thể dễ dàng tìm kiếm và quản lý các hóa đơn theo nhu cầu của mình.
        /// </returns>
        [HttpGet]
        [HasPermission(Permissions.Invoices.View)]
        [ProducesResponseType(typeof(Result<PagedResult<GetInvoicesResponse>>), 200)]
        public async Task<IActionResult> GetInvoicesAsync([FromQuery]PaginationParams pagination, [FromQuery] string? keyword, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var query = new GetInvoicesQuery
            {
                Pagination = pagination,
                Keyword = keyword,
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Tải xuống hóa đơn dưới dạng tệp PDF.
        /// Cho phép người dùng tải xuống bản sao của hóa đơn dưới dạng tệp PDF để lưu trữ hoặc in ấn.
        /// Endpoint này sẽ trả về tệp PDF chứa thông tin chi tiết của hóa đơn, bao gồm các mặt hàng đã mua, số lượng, giá cả, tổng cộng và thông tin khách hàng.
        /// Người dùng có thể sử dụng endpoint này để dễ dàng lưu trữ hoặc chia sẻ hóa đơn dưới dạng tệp PDF.
        /// </summary>
        /// <param name="id">
        /// ID của hóa đơn cần tải xuống dưới dạng PDF.
        /// Đây là một GUID duy nhất xác định hóa đơn mà người dùng muốn tải xuống.
        /// Endpoint này sẽ sử dụng ID này để tìm kiếm hóa đơn tương ứng trong hệ thống và tạo tệp PDF chứa thông tin chi tiết của hóa đơn đó.
        /// Người dùng cần cung cấp ID hợp lệ để có thể tải xuống hóa đơn dưới dạng PDF thành công.
        /// </param>
        /// <returns>
        /// Trả về tệp PDF chứa thông tin chi tiết của hóa đơn.
        /// Nếu hóa đơn tồn tại và người dùng có quyền truy cập, endpoint sẽ trả về tệp PDF với nội dung chi tiết của hóa đơn, bao gồm các mặt hàng đã mua, số lượng, giá cả, tổng cộng và thông tin khách hàng.
        /// Nếu hóa đơn không tồn tại hoặc người dùng không có quyền truy cập, endpoint sẽ trả về lỗi tương ứng (ví dụ: 404 Not Found hoặc 403 Forbidden).
        /// Người dùng có thể sử dụng tệp PDF này để lưu trữ hoặc in ấn hóa đơn theo nhu cầu của mình.
        /// </returns>
        [HttpGet("{id:guid}/pdf")]
        [HasPermission(Permissions.Invoices.ViewPdf)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoicePdf(Guid id)
        {
            var query = new GetInvoicePdfQuery(id);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult(result);
            }

            return File(result.Data, "application/pdf", $"Invoice_{id}.pdf");
        }

        /// <summary>
        /// Tạo hóa đơn mới cho một đơn hàng đã hoàn thành.
        /// Cho phép người dùng tạo hóa đơn mới dựa trên một đơn hàng đã hoàn thành.
        /// Endpoint này sẽ nhận thông tin về đơn hàng và số tiền đã nhận để tạo ra một hóa đơn mới trong hệ thống.
        /// Người dùng có thể sử dụng endpoint này để nhanh chóng tạo hóa đơn cho các đơn hàng đã hoàn thành, giúp quản lý tài chính và lưu trữ thông tin hóa đơn một cách hiệu quả.
        /// </summary>
        /// <param name="orderId">
        /// ID của đơn hàng đã hoàn thành mà hóa đơn sẽ được tạo ra.
        /// Đây là một GUID duy nhất xác định đơn hàng mà người dùng muốn tạo hóa đơn cho nó.
        /// Endpoint này sẽ sử dụng ID này để tìm kiếm đơn hàng tương ứng trong hệ thống và tạo hóa đơn mới dựa trên thông tin của đơn hàng đó.
        /// Người dùng cần cung cấp ID hợp lệ của đơn hàng đã hoàn thành để có thể tạo hóa đơn mới thành công.
        /// </param>
        /// <param name="amountReceived">
        /// Số tiền đã nhận từ khách hàng cho đơn hàng.
        /// Đây là một giá trị thập phân đại diện cho số tiền mà khách hàng đã thanh toán cho đơn hàng.
        /// Endpoint này sẽ sử dụng số tiền này để tính toán tổng số tiền của hóa đơn và lưu trữ thông tin về số tiền đã nhận trong hệ thống.
        /// Người dùng cần cung cấp số tiền hợp lệ để có thể tạo hóa đơn mới thành công và đảm bảo rằng thông tin tài chính được quản lý chính xác.
        /// </param>
        /// <returns>
        /// Trả về ID của hóa đơn mới được tạo.
        /// Nếu hóa đơn được tạo thành công, endpoint sẽ trả về một GUID duy nhất xác định hóa đơn mới trong hệ thống.
        /// Người dùng có thể sử dụng ID này để truy cập chi tiết của hóa đơn hoặc thực hiện các hành động liên quan đến hóa đơn trong tương lai.
        /// Nếu có lỗi xảy ra trong quá trình tạo hóa đơn (ví dụ: đơn hàng không tồn tại, số tiền không hợp lệ, v.v.), endpoint sẽ trả về lỗi tương ứng (ví dụ: 400 Bad Request) với thông tin chi tiết về lỗi để người dùng có thể hiểu và khắc phục vấn đề.
        /// </returns>
        [HttpPost]
        [HasPermission(Permissions.Invoices.Create)]
        [ProducesResponseType(typeof(Result<CreateInvoiceResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateInvoiceAsync(Guid orderId, decimal amountReceived)
        {
            var command = new CreateInvoiceCommand(orderId, amountReceived);
            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Data != null)
            {
                return CreatedAtAction(nameof(GetInvoicePdf), new { id = result.Data.InvoiceId }, result);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Xem chi tiết của một hóa đơn cụ thể bằng ID.
        /// Cho phép người dùng xem thông tin chi tiết của một hóa đơn cụ thể dựa trên ID của nó.
        /// Endpoint này sẽ trả về thông tin chi tiết của hóa đơn, bao gồm các mặt hàng đã mua, số lượng, giá cả, tổng cộng và thông tin khách hàng.
        /// Người dùng có thể sử dụng endpoint này để xem lại chi tiết của một hóa đơn cụ thể, kiểm tra thông tin liên quan đến hóa đơn đó và theo dõi trạng thái của nó (ví dụ: đã thanh toán, đang xử lý, v.v.).
        /// Endpoint này yêu cầu người dùng có quyền truy cập để đảm bảo rằng chỉ những người dùng có quyền mới có thể xem chi tiết của hóa đơn.
        /// </summary>
        /// <param name="id">ID của hóa đơn cần xem chi tiết.</param>
        /// <returns>Thông tin chi tiết của hóa đơn.</returns>
        [HttpGet("{id:guid}")]
        [HasPermission(Permissions.Invoices.View)]
        [ProducesResponseType(typeof(Result<GetInvoiceByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoiceById(Guid id)
        {
            var query = new GetInvoiceByIdQuery(id);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult(result);
            }

            return HandleResult(result);
        }
    }
}
