using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Interfaces;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Models.Reports;
using xQuantum_API.Repositories;

namespace xQuantum_API.Controllers.Reports
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]/[action]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<CustomersController> _logger;

        public InventoryController(IInventoryService inventoryService, IAuthenticationService authService, ILogger<CustomersController> logger)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        /// <summary>
        /// Get tenant ID from current user context
        /// </summary>
        private string GetOrgId()
        {
            var orgId = HttpContext.Items["OrgId"]?.ToString()
                ?? _authService.GetOrgIdFromToken(User);

            if (string.IsNullOrWhiteSpace(orgId))
            {
                throw new UnauthorizedAccessException("Tenant ID not found in context");
            }

            return orgId;
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetInventory([FromBody] InventoryQueryRequest req)
        {
            var response = await _inventoryService.GetInventoryAsync(GetOrgId(), req);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }

}
