using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Interfaces;
using xQuantum_API.Models.Common;
using xQuantum_API.Models.UserRegistration;

namespace xQuantum_API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UserRegistrationController : ControllerBase
    {
        private readonly IUserRegistrationService _service;

        public UserRegistrationController(IUserRegistrationService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail("Invalid request data."));

            var response = await _service.RegisterUserAsync(req);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // 🔹 2️⃣ Verify OTP
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
        {
            if (req == null || req.UserId == Guid.Empty || string.IsNullOrWhiteSpace(req.OtpCode))
                return BadRequest(ApiResponse<string>.Fail("UserId and OTP are required."));

            var response = await _service.VerifyOtpAsync(req);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest req)
        {
            if (req == null || req.UserId == Guid.Empty || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(ApiResponse<string>.Fail("UserId and Password are required."));

            var response = await _service.SetPasswordAsync(req);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }

}
