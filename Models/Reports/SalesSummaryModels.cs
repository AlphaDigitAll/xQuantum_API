namespace xQuantum_API.Models.Reports
{
    /// <summary>
    /// Request model for fetching sales summary data
    /// </summary>
    public enum SummaryTabType
    {
        DateAndTime = 1,
        Product = 2,     
        Demographic = 3,  
        Shipping = 4,     
        Promotion = 5      
    }

    public class SummaryFilterRequest
    {
        public Guid SubId { get; set; }
        public int TabType { get; set; } // numeric tab from frontend (1–5)
        public string TableName { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string SortField { get; set; } = "order_date";
        public string SortOrder { get; set; } = "DESC";
        public string? GlobalSearch { get; set; }
        public object? Filters { get; set; }

        public string LoadTypeText =>
            TabType switch
            {
                (int)SummaryTabType.DateAndTime => "dateandtime",
                (int)SummaryTabType.Product => "product",
                (int)SummaryTabType.Demographic => "demographic",
                (int)SummaryTabType.Shipping => "shipping",
                (int)SummaryTabType.Promotion => "promotion",
                _ => "order"
            };
    }


    /// <summary>
    /// Request model for fetching sales graph aggregate data (no pagination)
    /// </summary>
    public class GraphFilterRequest
    {
        public Guid SubId { get; set; }
        public string ChartName { get; set; } = string.Empty;
        public string TabType { get; set; } = string.Empty;
        public Dictionary<string, string>? Filters { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
