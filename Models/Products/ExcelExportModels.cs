using System.ComponentModel.DataAnnotations;

namespace xQuantum_API.Models.Products
{
    /// <summary>
    /// Request for exporting products with custom columns to Excel
    /// </summary>
    public class ExportProductsToExcelRequest
    {
        [Required(ErrorMessage = "SubId is required")]
        public Guid SubId { get; set; }

        public Guid? ProfileId { get; set; }
    }
}
