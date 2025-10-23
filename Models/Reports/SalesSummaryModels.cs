namespace xQuantum_API.Models.Reports
{
 
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
        public int TabType { get; set; } 
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

    public class SummaryCardRequest
    {
        public Guid SubId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
       
    }


    public class GraphFilterRequest
    {
        public Guid SubId { get; set; }

        public string ChartName { get; set; } = string.Empty;
        public int TabType { get; set; }
        public Dictionary<string, string>? Filters { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string LoadTypeText =>
            ((SummaryTabType)TabType) switch
            {
                SummaryTabType.DateAndTime => "dateandtime",
                SummaryTabType.Product => "product",
                SummaryTabType.Demographic => "demographic",
                SummaryTabType.Shipping => "shipping",
                SummaryTabType.Promotion => "promotion",
                _ => "order"
            };
    }
}
