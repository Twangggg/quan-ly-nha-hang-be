namespace FoodHub.Application.Common.Models;

/// <summary>
/// Lớp đại diện cho kết quả phân trang (Paged Result)
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu của các mục trong trang</typeparam>
public class PagedResult<T>
{
    // Danh sách các mục dữ liệu trong trang hiện tại
    public IReadOnlyList<T> Items { get; set; }
    
    // Tổng số lượng bản ghi trong toàn bộ database (trước khi phân trang)
    public int TotalCount { get; set; }
    
    // Số thứ tự trang hiện tại (bắt đầu từ 1)
    public int PageNumber { get; set; }
    
    // Số lượng bản ghi trên một trang
    public int PageSize { get; set; }
    
    // Tổng số trang = Tổng số bản ghi / Số bản ghi mỗi trang (làm tròn lên)
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    
    // Kiểm tra xem có trang tiếp theo hay không
    public bool HasNext => PageNumber < TotalPages;
    
    // Kiểm tra xem có trang trước đó hay không
    public bool HasPrevious => PageNumber > 1;

    public PagedResult(IReadOnlyList<T> items, PaginationParams pagination, int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pagination.PageNumber;
        PageSize = pagination.PageSize;
    }
}
