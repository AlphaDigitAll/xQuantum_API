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
    public class InventoryCardSummaryRequest
    {
        public Guid SubId { get; set; }
    }
    public class InventoryCardSummary
    {
        public Guid sub_id { get; set; }
        public int fulfillable_quantity { get; set; }
        public int working_quantity { get; set; }
        public int shipped_quantity { get; set; }
        public int receiving_quantity { get; set; }
        public int unfulfillable_quantity { get; set; }
        public int total_reserved_quantity { get; set; }
        public int customer_order_quantity { get; set; }
        public int trans_shipment_quantity { get; set; }
        public int fc_processing_quantity { get; set; }
        public int customer_damaged_quantity { get; set; }
        public int warehouse_damaged_quantity { get; set; }
        public int distributor_damaged_quantity { get; set; }
        public int carrier_damaged_quantity { get; set; }
        public int defective_quantity { get; set; }
        public int expired_quantity { get; set; }
    }

}
