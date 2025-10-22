namespace xQuantum_API.Models.Products
{
    /// <summary>
    /// Request model for bulk upserting product columns and values from Excel
    /// </summary>
    public class BulkProductColumnUpsertRequest
    {
        public Guid SubId { get; set; }
        public Guid ProfileId { get; set; }
        public Guid CreatedBy { get; set; }
        public IFormFile ExcelFile { get; set; } = null!;
    }

    /// <summary>
    /// Response model for bulk upsert operation
    /// </summary>
    public class BulkProductColumnUpsertResponse
    {
        public int ColumnsUpserted { get; set; }
        public int ValuesUpserted { get; set; }
        public int ProductsProcessed { get; set; }
        public double ElapsedSeconds { get; set; }
        public List<string> ProcessedColumns { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
