using System.Text.Json;

namespace xQuantum_API.Helpers
{
    /// <summary>
    /// Utility class for building consistent JSON responses across services
    /// </summary>
    public static class JsonResponseBuilder
    {
        /// <summary>
        /// Builds a standard error JSON response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>JSON string with error response</returns>
        public static string BuildErrorJson(string message)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = message,
                data = (object?)null
            });
        }

        /// <summary>
        /// Builds a bulk operation error JSON response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>JSON string with bulk error response</returns>
        public static string BuildBulkErrorJson(string message)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = message,
                columnsProcessed = 0,
                valuesUpserted = 0,
                productCount = 0,
                elapsedSeconds = 0.0,
                processedColumns = Array.Empty<string>()
            });
        }

        /// <summary>
        /// Builds a standard success JSON response
        /// </summary>
        /// <param name="data">Success data object</param>
        /// <param name="message">Success message (default: "Success")</param>
        /// <returns>JSON string with success response</returns>
        public static string BuildSuccessJson(object data, string message = "Success")
        {
            return JsonSerializer.Serialize(new
            {
                success = true,
                message = message,
                data = data
            });
        }
    }
}
