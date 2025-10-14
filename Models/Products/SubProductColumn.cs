using Newtonsoft.Json.Linq;

namespace xQuantum_API.Models.Products
{
    public class SubProductColumn
    {
        public int Id { get; set; }
        public Guid SubId { get; set; }
        public string ColumnName { get; set; }
        public Guid ProfileId { get; set; }
        public bool IsActive { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
    public class ProductDetail
    {
        public string product_asin { get; set; } = string.Empty;
        public string product_name { get; set; } = string.Empty;
        public string product_image { get; set; } = string.Empty;
        public Dictionary<string, object> dynamic_fields { get; set; } = new();
    }


}
