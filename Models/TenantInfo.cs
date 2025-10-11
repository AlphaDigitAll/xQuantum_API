namespace xQuantum_API.Models
{
    public class TenantInfo
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string OrgId { get; set; }
        public string OrgName { get; set; }
        public bool IsSellerLWA { get; set; }
        public bool IsVendorLWA { get; set; }
        public bool IsAdsLWA { get; set; }
    }
}
