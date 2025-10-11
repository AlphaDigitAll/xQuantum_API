using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Interfaces;

namespace xQuantum_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ICustomerService customerService, IAuthenticationService authService,ILogger<CustomersController> logger)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(_customerService));
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
        /// <summary>
        /// Get customer by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(long id)
        {
            try
            {
                var orgId = GetOrgId();
                _logger.LogInformation("Fetching customer {CustomerId} for tenant: {OrgId}", id, orgId);

                var customer = 1;//await _customerRepository.GetCustomerByIdAsync(orgId, id);

                if (customer == null)
                {
                    return NotFound(new { message = "Customer not found" });
                }

                return Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer {CustomerId}", id);
                throw;
            }
        }
    }
}
