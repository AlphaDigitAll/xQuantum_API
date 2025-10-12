namespace xQuantum_API.Models.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success")
            => new ApiResponse<T> { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message)
            => new ApiResponse<T> { Success = false, Message = message };
    }
    public class PaginatedResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalRecords { get; set; }
        public List<T> Records { get; set; } = new();
    }
    public class PaginatedResponseWithFooter<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalRecords { get; set; }
        public List<T> Records { get; set; }
        public Dictionary<string, object> Footer { get; set; }
    }
}
