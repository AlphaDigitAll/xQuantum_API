using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Interfaces.Subscription;
using xQuantum_API.Models.Subscription;

namespace xQuantum_API.Controllers.Subscription
{
    /// <summary>
    /// Controller for managing seller subscriptions (Amazon seller account connections)
    /// All endpoints require authentication and automatically use tenant context from JWT
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SellerSubscriptionController : TenantAwareControllerBase
    {
        private readonly ISellerSubscriptionService _service;
        private readonly ILogger<SellerSubscriptionController> _logger;

        public SellerSubscriptionController(
            ISellerSubscriptionService service,
            ILogger<SellerSubscriptionController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a new seller subscription
        /// POST /api/SellerSubscription
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation("Creating new seller subscription for OrgId: {OrgId}", OrgId);

            var response = await _service.CreateSubscriptionAsync(OrgId, request, UserIdGuid);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Authenticate seller with Amazon and save access tokens
        /// POST /api/SellerSubscription/authenticate
        /// </summary>
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateSellerRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation("Authenticating seller for SubId: {SubId}, OrgId: {OrgId}",
                request.SubId, OrgId);

            var response = await _service.AuthenticateAndSaveSellerDataAsync(OrgId, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Get all subscriptions for current organization
        /// GET /api/SellerSubscription
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Getting all subscriptions for OrgId: {OrgId}", OrgId);

            var response = await _service.GetAllSubscriptionsAsync(OrgId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Get detailed subscription information
        /// GET /api/SellerSubscription/details
        /// </summary>
        [HttpGet("details")]
        public async Task<IActionResult> GetDetails()
        {
            _logger.LogInformation("Getting subscription details for OrgId: {OrgId}", OrgId);

            var response = await _service.GetSubscriptionDetailsAsync(OrgId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Get subscription by ID
        /// GET /api/SellerSubscription/{subId}
        /// </summary>
        [HttpGet("{subId:guid}")]
        public async Task<IActionResult> GetById(Guid subId)
        {
            _logger.LogInformation("Getting subscription {SubId} for OrgId: {OrgId}", subId, OrgId);

            var response = await _service.GetSubscriptionByIdAsync(OrgId, subId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Get all countries for organization subscriptions
        /// GET /api/SellerSubscription/countries
        /// </summary>
        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries()
        {
            _logger.LogInformation("Getting countries for OrgId: {OrgId}", OrgId);

            var response = await _service.GetAllCountriesAsync(OrgId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Get marketplace information by region
        /// GET /api/SellerSubscription/marketplaces/{region}
        /// </summary>
        [HttpGet("marketplaces/{region}")]
        public async Task<IActionResult> GetMarketplacesByRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                return BadRequest(new { success = false, message = "Region parameter is required" });

            _logger.LogInformation("Getting marketplaces for region: {Region}", region);

            var response = await _service.GetMarketplacesByRegionAsync(region);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Check if subscription exists
        /// GET /api/SellerSubscription/exists?sellingPartnerId={sellingPartnerId}&mwsAuthToken={mwsAuthToken}
        /// </summary>
        [HttpGet("exists")]
        public async Task<IActionResult> CheckExists(
            [FromQuery] string sellingPartnerId,
            [FromQuery] string mwsAuthToken)
        {
            if (string.IsNullOrWhiteSpace(sellingPartnerId) || string.IsNullOrWhiteSpace(mwsAuthToken))
                return BadRequest(new { success = false, message = "SellingPartnerId and MwsAuthToken are required" });

            _logger.LogInformation("Checking if subscription exists for OrgId: {OrgId}", OrgId);

            var exists = await _service.SubscriptionExistsAsync(OrgId, sellingPartnerId, mwsAuthToken);
            return Ok(new { success = true, exists = exists });
        }
    }
}
