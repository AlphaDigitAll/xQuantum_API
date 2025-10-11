using Microsoft.AspNetCore.Identity.Data;
using xQuantum_API.Models.UserRegistration;

namespace xQuantum_API.Interfaces
{
    public interface IUserRegistrationService
    {
        Task<Guid> RegisterUserAsync(RegisterUserRequest request);
        Task<bool> VerifyOtpAsync(VerifyOtpRequest request);
        Task<bool> SetPasswordAsync(SetPasswordRequest request);
    }
}
