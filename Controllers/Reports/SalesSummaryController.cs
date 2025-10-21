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
        [HttpPost("summary")]
        [Produces("application/json")]
        public async Task<IActionResult> GetSummary([FromBody] SummaryFilterRequest req)
        {
            if (req is null || req.SubId == Guid.Empty || string.IsNullOrWhiteSpace(req.TabType) || string.IsNullOrWhiteSpace(req.TableName))
                return BadRequest(new { error = "subId, tabType and loadLevel are required" });

            req.Page = Math.Max(req.Page, 1);
            req.PageSize = (req.PageSize <= 0 || req.PageSize > 1000) ? 100 : req.PageSize;
            req.TabType = req.TabType.ToLowerInvariant();
            req.TableName = req.TableName.ToLowerInvariant();

            try
            {
                var json = await _salesSummaryService.GetSellerSalesSummaryJsonAsync(HttpContext.Items["OrgId"]?.ToString() ?? string.Empty, req);
                if (string.IsNullOrWhiteSpace(json)) return NotFound(new { error = "no data" });
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSummary failed");
                return StatusCode(500, new { error = "internal error" });
            }
        }
    }
}
