using System.ComponentModel.DataAnnotations;

namespace xQuantum_API.Models.Products
{
    /// <summary>
    /// Product blacklist keyword entity for storing negative exact and phrase matches
    /// Optimized for ultra-fast bulk operations
    /// </summary>
    public class ProductBlacklistKeyword
    {
        public int Id { get; set; }

        [Required]
        public Guid SubId { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 1)]
        public string Asin { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated list of exact match keywords to exclude
        /// Example: "asdfgh,zxcvbn,fdffe"
        /// </summary>
        public string? NegativeExact { get; set; }

        /// <summary>
        /// Comma-separated list of phrase match keywords to exclude
        /// Example: "vfdgfdgdgd,qwerty"
        /// </summary>
        public string? NegativePhrase { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public Guid CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public Guid? UpdatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
    }

    /// <summary>
    /// Response model for GetBlacklistData endpoint
    /// Includes product details from tbl_amz_products
    /// </summary>
    public class ProductBlacklistResponse
    {
        public int Id { get; set; }

        [Required]
        public string ProductTitle { get; set; } = string.Empty;

        public string? Image { get; set; }

        [Required]
        public string ASIN { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated exact match keywords
        /// </summary>
        public string? NegativeExact { get; set; }

        /// <summary>
        /// Comma-separated phrase match keywords
        /// </summary>
        public string? NegativePhrase { get; set; }
    }

    /// <summary>
    /// Request model for bulk updating blacklist values
    /// Supports ultra-fast bulk operations (100+ records in <50ms)
    /// </summary>
    public class UpdateBlacklistValueRequest
    {
        [Required]
        public Guid SubId { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 1)]
        public string Asin { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string ColumnKey { get; set; } = string.Empty; // "negative_exact" or "negative_phrase"

        public string? ColumnValue { get; set; }
    }

    /// <summary>
    /// Bulk request model for updating multiple blacklist values
    /// </summary>
    public class BulkUpdateBlacklistValuesRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        [MaxLength(10000, ErrorMessage = "Maximum 10,000 items allowed per request")]
        public List<UpdateBlacklistValueRequest> Items { get; set; } = new();
    }
}
