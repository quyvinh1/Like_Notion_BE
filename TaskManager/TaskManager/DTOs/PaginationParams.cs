namespace TaskManager.DTOs
{
    public class PaginationParams
    {
        private const int MaxPageSize = 50;
        public int PageNumber { get; set; } = 1;
        private int _pageSize = 10;
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = (value > MaxPageSize) ? MaxPageSize : value; }
        }
        public string? SortBy { get; set; } = "Id";
        public string? SortOrder { get; set;} = "desc";
    }
}
