using Microsoft.AspNetCore.Identity.Data;
using xQuantum_API.Models;
using xQuantum_API.Models.UserRegistration;

namespace xQuantum_API.Interfaces
{
    public interface IUserRegistrationService
    {
        Task<ApiResponse<Guid>> RegisterUserAsync(RegisterUserRequest request);
        Task<ApiResponse<bool>> VerifyOtpAsync(VerifyOtpRequest request);
        Task<ApiResponse<bool>> SetPasswordAsync(SetPasswordRequest request);
    }
}
