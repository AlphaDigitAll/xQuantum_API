namespace xQuantum_API.Models.Reports
{
    /// <summary>
    /// Request model for fetching sales summary data
    /// </summary>
    public class SummaryFilterRequest
    {
        public Guid SubId { get; set; }
        public string LoadLevel { get; set; } = string.Empty;
        public string TabType { get; set; } = string.Empty;
        public Dictionary<string, string> Filters { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Offset { get; set; } = 0;
        public int Limit { get; set; } = 100;
    }

    /// <summary>
    /// Request model for fetching sales graph aggregate data (no pagination)
    /// </summary>
    public class GraphFilterRequest
    {
        public Guid SubId { get; set; }
        public string LoadLevel { get; set; } = string.Empty;
        public string TabType { get; set; } = string.Empty;
        public Dictionary<string, string> Filters { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
