namespace xQuantum_API.Models.Reports
{
    /// <summary>
    /// Request model for querying inventory data with filtering and pagination
    /// </summary>
    public class InventoryQueryRequest
    {
        public Guid SubId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortField { get; set; } = "id"; // default
        public int SortOrder { get; set; } = 1; // 1 = ASC, -1 = DESC
        public string? GlobalSearch { get; set; } = null;
        public Dictionary<string, object> Filters { get; set; } = new();
    }

    /// <summary>
    /// Request model for inventory card summary
    /// </summary>
    public class InventoryCardSummaryRequest
    {
        public Guid SubId { get; set; }
    }
}
