using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Interfaces.MasterData;

namespace xQuantum_API.Controllers.MasterData
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MasterDataController : ControllerBase
    {
        private readonly IMasterDataService _service;

        public MasterDataController(IMasterDataService service)
        {
            _service = service;
        }

        [HttpGet("business")]
        public async Task<IActionResult> GetBusiness([FromQuery] string search = "")
        {
            var result = await _service.GetBusinessTypesAsync(search);
            return Ok(result);
        }

        [HttpGet("cities")]
        public async Task<IActionResult> GetCities([FromQuery] string countryId, [FromQuery] string search = "")
        {
            if (string.IsNullOrWhiteSpace(countryId))
                return BadRequest("countryId is required.");

            var result = await _service.GetCitiesAsync(countryId, search);
            return Ok(result);
        }


        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries([FromQuery] string search = "")
        {
            var result = await _service.GetCountriesAsync(search);
            return Ok(result);
        }

        [HttpGet("mobile-code")]
        public async Task<IActionResult> GetMobileCountriesCodeAsync([FromQuery] string search = "")
        {
            var result = await _service.GetMobileCountriesCodeAsync(search);
            return Ok(result);
        }

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans([FromQuery] string search = "")
        {
            var result = await _service.GetPlansAsync(search);
            return Ok(result);
        }
    }
}