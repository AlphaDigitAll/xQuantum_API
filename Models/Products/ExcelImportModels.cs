using System.ComponentModel.DataAnnotations;

namespace xQuantum_API.Models.Products
{
    /// <summary>
    /// Request for Excel upload - sent as form data
    /// </summary>
    public class BulkUpsertFromExcelRequest
    {
        [Required(ErrorMessage = "SubId is required")]
        public Guid SubId { get; set; }

        [Required(ErrorMessage = "ProfileId is required")]
        public Guid ProfileId { get; set; }

        [Required(ErrorMessage = "Excel file is required")]
        public IFormFile ExcelFile { get; set; } = null!;
    }

    /// <summary>
    /// Response for Excel bulk upsert
    /// </summary>
    public class BulkUpsertFromExcelResponse
    {
        public int ColumnsProcessed { get; set; }
        public int ValuesUpserted { get; set; }
        public int ProductsProcessed { get; set; }
        public double ElapsedSeconds { get; set; }
        public List<string> ProcessedColumns { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
