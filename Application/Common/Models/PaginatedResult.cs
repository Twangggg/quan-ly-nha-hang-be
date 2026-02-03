namespace FoodHub.Application.Common.Models
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedResult(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            TotalCount = count;
            Items = items;
        }

        public static PaginatedResult<T> Create(List<T> items, int count, int pageIndex, int pageSize)
        {
            return new PaginatedResult<T>(items, count, pageIndex, pageSize);
        }
    }
}
