using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Interfaces;
using xQuantum_API.Interfaces.Products;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Models.Products;

namespace xQuantum_API.Controllers.Products
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubProductColumnController : TenantAwareControllerBase
    {
        private readonly ISubProductColumnService _service;
        private readonly ILogger<SubProductColumnController> _logger;

        public SubProductColumnController(ISubProductColumnService service, ILogger<SubProductColumnController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a new sub-product column
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubProductColumnRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Map request to entity with system-managed fields
            var model = new SubProductColumn
            {
                SubId = request.SubId,
                ColumnName = request.ColumnName,
                ProfileId = request.ProfileId,
                CreatedBy = UserIdGuid,  // From JWT token
                CreatedOn = DateTime.UtcNow,  // Set automatically
                IsActive = true  // Set automatically
            };

            var response = await _service.InsertAsync(OrgId, model);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Update an existing sub-product column
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateSubProductColumnRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Map request to entity with system-managed fields
            var model = new SubProductColumn
            {
                Id = request.Id,
                SubId = request.SubId,
                ColumnName = request.ColumnName,
                ProfileId = request.ProfileId,
                UpdatedBy = UserIdGuid,  // From JWT token
                UpdatedOn = DateTime.UtcNow  // Set automatically
            };

            var response = await _service.UpdateAsync(OrgId, model);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Soft delete a sub-product column (sets IsActive = false)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _service.DeleteAsync(OrgId, id, UserIdGuid);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Get all columns for a specific sub-product
        /// </summary>
        [HttpGet("sub/{subId}")]
        public async Task<IActionResult> GetBySubId(Guid subId)
        {
            var response = await _service.GetBySubIdAsync(OrgId, subId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Get all columns for a specific profile
        /// </summary>
        [HttpGet("profile/{profileId}")]
        public async Task<IActionResult> GetByProfileId(Guid profileId)
        {
            var response = await _service.GetByProfileIdAsync(OrgId, profileId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        /// <summary>
        /// Get all products columns and value for a specific sub-id
        /// </summary>
        [HttpGet("Getproducts/{subId}")]
        public async Task<IActionResult> GetProductsBySubId(Guid subId)
        {
            var response = await _service.GetProductsBySubIdAsync(OrgId, subId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
