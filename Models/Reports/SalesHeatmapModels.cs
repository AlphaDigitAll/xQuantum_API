using System;
using System.ComponentModel.DataAnnotations;

namespace xQuantum_API.Models.Reports
{
    /// <summary>
    /// Enum for selecting which metric to display in the heatmap
    /// </summary>
    public enum TabType
    {
        NoOfOrder = 1,
        AvgOrder = 2,
        GrossSales = 3
    }

    /// <summary>
    /// Request model for sales heatmap data
    /// </summary>
    public class SalesHeatmapRequest
    {
        [Required]
        public TabType TabType { get; set; }

        [Required]
        public Guid SubId { get; set; }

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }
    }

    /// <summary>
    /// Heatmap data structure
    /// Returns a 7x24 array where:
    /// - First dimension (7 elements) = Days of week (Monday=0, Tuesday=1, ..., Sunday=6)
    /// - Second dimension (24 elements) = Hours of day (0-23)
    /// - Each inner array contains comma-separated values for that day
    /// </summary>
    public class SalesHeatmapData
    {
        public string[][] DayHourData { get; set; } = new string[7][];
    }
}
