using System.ComponentModel.DataAnnotations;

namespace xQuantum_API.Models.Products
{
    /// <summary>
    /// Request model for retrieving product COGS data with pagination, sorting, and search
    /// </summary>
    public class ProductCogsRequest
    {
        /// <summary>
        /// Subscription ID (tenant identifier)
        /// </summary>
        [Required(ErrorMessage = "SubId is required")]
        public Guid SubId { get; set; }

        /// <summary>
        /// Page number (1-based indexing)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of records per page (max 1000)
        /// </summary>
        public int PageSize { get; set; } = 100;

        /// <summary>
        /// Field to sort by (id, asin, product_title, fob, end_to_end, import_duty, created_on)
        /// </summary>
        public string SortField { get; set; } = "asin";

        /// <summary>
        /// Sort order: 0 = Ascending, 1 = Descending
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Global search string (searches across ASIN, product title, FOB, end_to_end, import_duty)
        /// </summary>
        public string GlobalSearch { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response data structure for product COGS records
    /// </summary>
    public class ProductCogsData
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public List<ProductCogsRecord> Records { get; set; } = new List<ProductCogsRecord>();
    }

    /// <summary>
    /// Individual product COGS record
    /// </summary>
    public class ProductCogsRecord
    {
        public int Id { get; set; }
        public string Asin { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Fob { get; set; } = string.Empty;
        public string EndToEnd { get; set; } = string.Empty;
        public string ImportDuty { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }

    /// <summary>
    /// Request model for bulk updating product COGS values
    /// Supports ultra-fast bulk operations (100+ records in <50ms)
    /// </summary>
    public class UpdateProductCogsRequest
    {
        [Required(ErrorMessage = "SubId is required")]
        public Guid SubId { get; set; }

        [Required(ErrorMessage = "ASIN is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "ASIN must be between 1 and 20 characters")]
        public string Asin { get; set; } = string.Empty;

        [Required(ErrorMessage = "ColumnKey is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "ColumnKey must be between 1 and 50 characters")]
        public string ColumnKey { get; set; } = string.Empty; // "fob", "end_to_end", or "import_duty"

        public string? ColumnValue { get; set; }
    }

    /// <summary>
    /// Bulk request model for updating multiple product COGS values
    /// </summary>
    public class BulkUpdateProductCogsRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        [MaxLength(10000, ErrorMessage = "Maximum 10,000 items allowed per request")]
        public List<UpdateProductCogsRequest> Items { get; set; } = new();
    }
}
