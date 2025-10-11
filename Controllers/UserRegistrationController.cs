using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using xQuantum_API.Interfaces;
using xQuantum_API.Models.UserRegistration;

namespace xQuantum_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserRegistrationController : ControllerBase
    {
        private readonly IUserRegistrationService _service;

        public UserRegistrationController(IUserRegistrationService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest req)
        {
            var userId = await _service.RegisterUserAsync(req);
            return Ok(new { Message = "OTP sent successfully", UserId = userId });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
        {
            bool success = await _service.VerifyOtpAsync(req);
            if (!success) return BadRequest("Invalid or expired OTP.");
            return Ok(new { Message = "OTP verified successfully." });
        }

        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest req)
        {
            bool success = await _service.SetPasswordAsync(req);
            if (!success) return BadRequest("Failed to set password.");
            return Ok(new { Message = "Password set successfully." });
        }
    }

}
