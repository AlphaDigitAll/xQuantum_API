using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Models.Reports;

namespace xQuantum_API.Controllers.Reports
{
    /// <summary>
    /// ULTRA-OPTIMIZED Controller for sales summary operations
    /// Returns JSON directly from database with ZERO conversion overhead
    /// Fastest possible response time
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]/[action]")]
    public class SalesSummaryController : TenantAwareControllerBase
    {
        private readonly ISalesSummaryService _salesSummaryService;
        private readonly ILogger<SalesSummaryController> _logger;

        public SalesSummaryController(
            ISalesSummaryService salesSummaryService,
            ILogger<SalesSummaryController> logger)
        {
            _salesSummaryService = salesSummaryService ?? throw new ArgumentNullException(nameof(salesSummaryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// ULTRA-FAST endpoint - Returns JSON directly from PostgreSQL
        /// NO C# conversion, NO object mapping, NO serialization overhead
        /// Database does all the work, C# just passes it through
        /// </summary>
        /// <param name="request">Filter and pagination parameters</param>
        /// <returns>Raw JSON string with paginated sales summary data</returns>
        /// <remarks>
        /// Sample Request:
        /// POST /api/SalesSummaryOptimized/GetSummary
        /// {
        ///   "tabType": "order",
        ///   "loadLevel": "day",
        ///   "subId": "47b8f57d-f59b-4e80-a6ac-d842e1520ff8",
        ///   "filters": {
        ///     "day": "2025-10-13"
        ///   },
        ///   "fromDate": "2025-10-01",
        ///   "toDate": "2025-10-31",
        ///   "offset": 0,
        ///   "limit": 100
        /// }
        ///
        /// Response Format (from database):
        /// {
        ///   "success": true,
        ///   "message": "Success",
        ///   "data": {
        ///     "page": 1,
        ///     "pageSize": 100,
        ///     "totalRecords": 450,
        ///     "records": [...]
        ///   }
        /// }
        /// </remarks>
        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> GetSummary([FromBody] SummaryFilterRequest request)
        {
            // Quick validation - fail fast
            if (request == null)
            {
                _logger.LogWarning("GetSummary called with null request");
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (request.SubId == Guid.Empty)
            {
                _logger.LogWarning("GetSummary called with empty SubId");
                return BadRequest(new { success = false, message = "SubId is required." });
            }

            if (string.IsNullOrWhiteSpace(request.LoadLevel))
            {
                _logger.LogWarning("GetSummary called with empty LoadLevel");
                return BadRequest(new { success = false, message = "LoadLevel is required." });
            }

            if (string.IsNullOrWhiteSpace(request.TabType))
            {
                _logger.LogWarning("GetSummary called with empty TabType");
                return BadRequest(new { success = false, message = "TabType is required." });
            }

            if (string.IsNullOrWhiteSpace(OrgId))
            {
                _logger.LogError("OrgId is missing from context");
                return BadRequest(new { success = false, message = "OrgId is missing." });
            }

            // Validate date range
            if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value > request.ToDate.Value)
            {
                return BadRequest(new { success = false, message = "FromDate cannot be greater than ToDate." });
            }

            // Validate pagination
            if (request.Offset < 0)
            {
                return BadRequest(new { success = false, message = "Offset cannot be negative." });
            }

            if (request.Limit <= 0 || request.Limit > 1000)
            {
                return BadRequest(new { success = false, message = "Limit must be between 1 and 1000." });
            }

            _logger.LogInformation(
                "Fetching sales summary for OrgId: {OrgId}, SubId: {SubId}, Module: {Module}, Level: {Level}",
                OrgId, request.SubId, request.TabType, request.LoadLevel);

            try
            {
                // Get JSON directly from database - ZERO conversion overhead!
                var jsonResult = await _salesSummaryService.GetSalesSummaryJsonAsync(OrgId, request);

                // Return raw JSON with proper content type
                // ASP.NET Core automatically sets Content-Type: application/json
                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error in GetSummary for OrgId: {OrgId}, SubId: {SubId}",
                    OrgId, request.SubId);

                return StatusCode(500, new
                {
                    success = false,
                    message = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }
    }
}
