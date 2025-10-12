namespace xQuantum_API.Models.Subscription
{
    /// <summary>
    /// Seller subscription entity representing Amazon seller account connection
    /// </summary>
    public class SellerSubscription
    {
        public Guid SubId { get; set; }
        public Guid OrgId { get; set; }
        public string? SubName { get; set; }
        public string? Region { get; set; }
        public string? AuthKey { get; set; }
        public DateTime? AuthOn { get; set; }
        public string? SpapiOauthCode { get; set; }
        public string? MwsAuthToken { get; set; }
        public string? SellingPartnerId { get; set; }
        public string? RefreshToken { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public bool OldDataLoad { get; set; }
        public string? Country { get; set; }
        public string? MarketplaceId { get; set; }
        public bool LoadingInProgress { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    /// <summary>
    /// Response model for subscription list
    /// </summary>
    public class SubscriptionListItem
    {
        public Guid SubId { get; set; }
        public string SubName { get; set; } = string.Empty;
        public bool OldDataLoad { get; set; }
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    /// <summary>
    /// Detailed subscription information
    /// </summary>
    public class SubscriptionDetail
    {
        public Guid SubId { get; set; }
        public string Region { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string SubName { get; set; } = string.Empty;
        public string SellingPartnerId { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public bool OldDataLoad { get; set; }
        public int Priority { get; set; }
    }

    /// <summary>
    /// Regional marketplace information
    /// </summary>
    public class RegionalMarketplace
    {
        public string Country { get; set; } = string.Empty;
        public string MarketplaceId { get; set; } = string.Empty;
        public string Gmt { get; set; } = string.Empty;
    }
}
