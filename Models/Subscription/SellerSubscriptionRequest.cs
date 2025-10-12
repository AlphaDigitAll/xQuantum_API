using System.ComponentModel.DataAnnotations;

namespace xQuantum_API.Models.Subscription
{
    /// <summary>
    /// Request to create a new seller subscription
    /// </summary>
    public class CreateSubscriptionRequest
    {
        [Required(ErrorMessage = "Subscription name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Subscription name must be between 2 and 200 characters")]
        public string SubName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Region is required")]
        [StringLength(50, ErrorMessage = "Region must not exceed 50 characters")]
        public string Region { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100, ErrorMessage = "Country must not exceed 100 characters")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "Marketplace ID is required")]
        [StringLength(50, ErrorMessage = "Marketplace ID must not exceed 50 characters")]
        public string MarketplaceId { get; set; } = string.Empty;

        [Required(ErrorMessage = "GMT timezone is required")]
        [StringLength(20, ErrorMessage = "GMT must not exceed 20 characters")]
        public string Gmt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to authenticate and save seller data from Amazon SP-API
    /// </summary>
    public class AuthenticateSellerRequest
    {
        [Required(ErrorMessage = "Subscription ID is required")]
        public Guid SubId { get; set; }

        [Required(ErrorMessage = "SP-API OAuth code is required")]
        public string SpapiOauthCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "MWS auth token is required")]
        public string MwsAuthToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selling partner ID is required")]
        public string SellingPartnerId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Access token response from Amazon SP-API
    /// </summary>
    public class AccessTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}
