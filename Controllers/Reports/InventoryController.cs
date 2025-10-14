using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Models.Common;
using xQuantum_API.Models.Reports;

namespace xQuantum_API.Controllers.Reports
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]/[action]")]
    public class InventoryController : TenantAwareControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetInventory([FromBody] InventoryQueryRequest req)
        {
            var response = await _inventoryService.GetInventoryAsync(OrgId, req);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        [HttpPost("summary")]
        public async Task<IActionResult> GetInventoryCard([FromBody] InventoryCardSummaryRequest req)
        {
            if (req == null || req.SubId == Guid.Empty)
                return BadRequest(new ApiResponse<string> { Success = false, Message = "SubId is required." });
            if (string.IsNullOrWhiteSpace(OrgId))
                return BadRequest(new ApiResponse<string> { Success = false, Message = "OrgId is missing or invalid." });
            var response = await _inventoryService.GetInventoryCardAsync(OrgId, req.SubId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }

}
