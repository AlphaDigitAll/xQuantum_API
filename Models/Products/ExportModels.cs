using Newtonsoft.Json;

namespace xQuantum_API.Models.Products
{
    /// <summary>
    /// Model for column information used during export operations
    /// </summary>
    public class ColumnInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("columnName")]
        public string ColumnName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model for product data row used during export operations
    /// </summary>
    public class ProductExportRow
    {
        [JsonProperty("asin")]
        public string Asin { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("image")]
        public string Image { get; set; } = string.Empty;

        [JsonProperty("columnValues")]
        public Dictionary<string, string>? ColumnValues { get; set; }
    }
}
