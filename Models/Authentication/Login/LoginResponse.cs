namespace xQuantum_API.Models.Authentication.Login
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public string OrgId { get; set; }
        public string OrgName { get; set; }
        public bool IsSellerLWA { get; set; }
        public bool IsVendorLWA { get; set; }
        public bool IsAdsLWA { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
