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

    /// <summary>
    /// Internal model for column data
    /// </summary>
    public class ProductColumnData
    {
        public int? Id { get; set; }
        public Guid SubId { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid CreatedBy { get; set; }
    }

    /// <summary>
    /// Internal model for column values
    /// </summary>
    public class ProductColumnValueData
    {
        public Guid SubId { get; set; }
        public string ProductAsin { get; set; } = string.Empty;
        public int ColumnId { get; set; }
        public string Value { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
    }

    /// <summary>
    /// Represents a row from the Excel file
    /// </summary>
    public class ExcelProductRow
    {
        public string ProductAsin { get; set; } = string.Empty;
        public Dictionary<string, string> ColumnValues { get; set; } = new();
    }
}
