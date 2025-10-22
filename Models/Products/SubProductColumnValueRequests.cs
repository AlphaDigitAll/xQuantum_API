using System.ComponentModel.DataAnnotations;

namespace xQuantum_API.Models.Products
{
    /// <summary>
    /// Request model for upserting (insert or update) a SubProductColumnValue.
    /// If a record with the same column_id, product_asin, and sub_id exists, it updates; otherwise inserts.
    /// </summary>
    public class UpsertSubProductColumnValueRequest
    {
        [Required(ErrorMessage = "SubId is required")]
        public Guid SubId { get; set; }

        [Required(ErrorMessage = "ProductAsin is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "ProductAsin must be between 1 and 50 characters")]
        public string ProductAsin { get; set; } = string.Empty;

        [Required(ErrorMessage = "ColumnId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ColumnId must be greater than 0")]
        public int ColumnId { get; set; }

        public string? Value { get; set; }
    }

    /// <summary>
    /// Single item for bulk upsert operation
    /// </summary>
    public class BulkUpsertColumnValueItem
    {
        [Required(ErrorMessage = "ProductAsin is required")]
        public string ProductAsin { get; set; } = string.Empty;

        [Required(ErrorMessage = "ColumnId is required")]
        public int ColumnId { get; set; }

        public string? Value { get; set; }
    }

    /// <summary>
    /// Request model for bulk upserting (insert or update) multiple SubProductColumnValues.
    /// Ultra-fast bulk operation using PostgreSQL UNNEST - can handle 1000+ records in milliseconds.
    /// </summary>
    public class BulkUpsertSubProductColumnValuesRequest
    {
        [Required(ErrorMessage = "SubId is required")]
        public Guid SubId { get; set; }

        [Required(ErrorMessage = "Items is required")]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<BulkUpsertColumnValueItem> Items { get; set; } = new();
    }
}
