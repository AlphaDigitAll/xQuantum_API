namespace xQuantum_API.Models.Products
{
    /// <summary>
    /// Internal model for column data used during bulk import operations
    /// </summary>
    internal class ProductColumnData
    {
        public int? Id { get; set; }
        public Guid SubId { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid CreatedBy { get; set; }
    }

    /// <summary>
    /// Internal model for column values used during bulk import operations
    /// </summary>
    internal class ProductColumnValueData
    {
        public Guid SubId { get; set; }
        public string ProductAsin { get; set; } = string.Empty;
        public int ColumnId { get; set; }
        public string Value { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
    }

    /// <summary>
    /// Represents a row from the Excel file during import
    /// </summary>
    internal class ExcelProductRow
    {
        public string ProductAsin { get; set; } = string.Empty;
        public Dictionary<string, string> ColumnValues { get; set; } = new();
    }
}
