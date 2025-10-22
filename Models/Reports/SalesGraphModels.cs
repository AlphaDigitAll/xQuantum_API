using xQuantum_API.Models.Common;

namespace xQuantum_API.Models.Reports
{
    /// <summary>
    /// Request model for sales graph data
    /// </summary>
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
