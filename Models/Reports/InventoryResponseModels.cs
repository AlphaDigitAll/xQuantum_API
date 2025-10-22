namespace xQuantum_API.Models.Reports
{
    /// <summary>
    /// Summary of inventory quantities grouped by status
    /// </summary>
    public class InventoryCardSummary
    {
        public Guid sub_id { get; set; }
        public FulfillableGroup Fulfillable_Quantity { get; set; } = new();
        public ReservedGroup Reserved_Quantity { get; set; } = new();
        public UnfulfillableGroup Unfulfillable_Quantity { get; set; } = new();
    }

    /// <summary>
    /// Fulfillable inventory quantities breakdown
    /// </summary>
    public class FulfillableGroup
    {
        public int fulfillable_quantity { get; set; }
        public int working_quantity { get; set; }
        public int shipped_quantity { get; set; }
        public int receiving_quantity { get; set; }
    }

    /// <summary>
    /// Reserved inventory quantities breakdown
    /// </summary>
    public class ReservedGroup
    {
        public int total_reserved_quantity { get; set; }
        public int customer_order_quantity { get; set; }
        public int trans_shipment_quantity { get; set; }
        public int fc_processing_quantity { get; set; }
    }

    /// <summary>
    /// Unfulfillable inventory quantities breakdown
    /// </summary>
    public class UnfulfillableGroup
    {
        public int unfulfillable_quantity { get; set; }
        public int customer_damaged_quantity { get; set; }
        public int warehouse_damaged_quantity { get; set; }
        public int distributor_damaged_quantity { get; set; }
        public int carrier_damaged_quantity { get; set; }
        public int defective_quantity { get; set; }
        public int expired_quantity { get; set; }
    }
}
