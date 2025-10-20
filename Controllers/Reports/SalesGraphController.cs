using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Models.Reports;

namespace xQuantum_API.Controllers.Reports
{
    /// <summary>
    /// ULTRA-OPTIMIZED Controller for sales graph aggregate data
    /// Returns JSON directly from database with ZERO conversion overhead
    /// Fastest possible response time for graph visualizations
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]/[action]")]
    public class SalesGraphController : TenantAwareControllerBase
    {
        private readonly ISalesGraphService _salesGraphService;
        private readonly ILogger<SalesGraphController> _logger;

        public SalesGraphController(
            ISalesGraphService salesGraphService,
            ILogger<SalesGraphController> logger)
        {
            _salesGraphService = salesGraphService ?? throw new ArgumentNullException(nameof(salesGraphService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// ULTRA-FAST endpoint - Returns aggregated graph data as JSON directly from PostgreSQL
        /// NO C# conversion, NO object mapping, NO serialization overhead
        /// Database does all aggregation and formatting, C# just passes it through
        /// </summary>
        /// <param name="request">Filter parameters (no pagination needed for aggregates)</param>
        /// <returns>Raw JSON string with aggregated sales data (total_sales, total_units, total_aov)</returns>
        /// <remarks>
        /// Sample Request:
        /// POST /api/SalesGraph/GetGraphData
        /// {
        ///   "tabType": "order",
        ///   "loadLevel": "day",
        ///   "subId": "47b8f57d-f59b-4e80-a6ac-d842e1520ff8",
        ///   "filters": {
        ///     "month": "2025-09-01"
        ///   },
        ///   "fromDate": "2025-08-01",
        ///   "toDate": "2025-10-31"
        /// }
        ///
        /// Response Format (from database):
        /// {
        ///   "success": true,
        ///   "message": "Success",
        ///   "data": {
        ///     "totalSales": 125000.50,
        ///     "totalUnits": 3450,
        ///     "totalAov": 36.23
        ///   }
        /// }
        /// </remarks>
        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> GetGraphData([FromBody] GraphFilterRequest request)
        {
            // Quick validation - fail fast
            if (request == null)
            {
                _logger.LogWarning("GetGraphData called with null request");
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (request.SubId == Guid.Empty)
            {
                _logger.LogWarning("GetGraphData called with empty SubId");
                return BadRequest(new { success = false, message = "SubId is required." });
            }

            if (string.IsNullOrWhiteSpace(request.LoadLevel))
            {
                _logger.LogWarning("GetGraphData called with empty LoadLevel");
                return BadRequest(new { success = false, message = "LoadLevel is required." });
            }

            if (string.IsNullOrWhiteSpace(request.TabType))
            {
                _logger.LogWarning("GetGraphData called with empty TabType");
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

            _logger.LogInformation(
                "Fetching sales graph aggregates for OrgId: {OrgId}, SubId: {SubId}, Module: {Module}, Level: {Level}",
                OrgId, request.SubId, request.TabType, request.LoadLevel);

            try
            {
                // Get JSON directly from database - ZERO conversion overhead!
                var jsonResult = await _salesGraphService.GetSalesGraphAggregatesJsonAsync(OrgId, request);

                // Return raw JSON with proper content type
                // ASP.NET Core automatically sets Content-Type: application/json
                return Content(jsonResult, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error in GetGraphData for OrgId: {OrgId}, SubId: {SubId}",
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
