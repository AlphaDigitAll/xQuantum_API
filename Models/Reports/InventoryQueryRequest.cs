namespace xQuantum_API.Models.Reports
{
    public class InventoryQueryRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortField { get; set; } = "id"; // default
        public int SortOrder { get; set; } = 1; // 1 = ASC, -1 = DESC
        public string? GlobalSearch { get; set; } = null;
        public Dictionary<string, object> Filters { get; set; } = new();
    }

}
