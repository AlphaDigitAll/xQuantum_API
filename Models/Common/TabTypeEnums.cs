namespace xQuantum_API.Models.Common
{
    /// <summary>
    /// Enum for sales summary and graph tab types
    /// Used to determine which data aggregation to display
    /// </summary>
    public enum SummaryTabType
    {
        DateAndTime = 1,
        Product = 2,
        Demographic = 3,
        Shipping = 4,
        Promotion = 5
    }

    /// <summary>
    /// Enum for selecting which metric to display in sales heatmap
    /// </summary>
    public enum HeatmapTabType
    {
        /// <summary>Number of orders</summary>
        NoOfOrder = 1,

        /// <summary>Average order value</summary>
        AvgOrder = 2,

        /// <summary>Gross sales amount</summary>
        GrossSales = 3
    }
}
