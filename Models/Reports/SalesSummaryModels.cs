namespace xQuantum_API.Models.Reports
{
    /// <summary>
    /// Request model for fetching sales summary data
    /// </summary>
    public class SummaryFilterRequest
    {
        public Guid SubId { get; set; }
        public string TabType { get; set; } = "order";
        public string LoadLevel { get; set; } = "day";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string? SortField { get; set; }
        public string? SortOrder { get; set; }
        public string? GlobalSearch { get; set; }
        public Dictionary<string, string>? Filters { get; set; } = new();
    }
}
