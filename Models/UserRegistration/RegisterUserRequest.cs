namespace xQuantum_API.Models.UserRegistration
{

    public class RegisterUserRequest
    {
        public string OrgName { get; set; }
        public string OrgCode { get; set; }
        public string LegalBusinessName { get; set; }
        public string RegistrationNumber { get; set; }
        public string BusinessAddress { get; set; }
        public string CountryId { get; set; }
        public string CityId { get; set; }
        public string BusinessType { get; set; }
        public string WebsiteUrl { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }
    }

    public class VerifyOtpRequest
    {
        public Guid UserId { get; set; }
        public string OtpCode { get; set; }
    }

    public class SetPasswordRequest
    {
        public Guid UserId { get; set; }
        public string Password { get; set; }
    }


}
