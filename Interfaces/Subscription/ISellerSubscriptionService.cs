using xQuantum_API.Models.Common;
using xQuantum_API.Models.Subscription;

namespace xQuantum_API.Interfaces.Subscription
{
    /// <summary>
    /// Service interface for managing seller subscriptions (Amazon seller accounts)
    /// </summary>
    public interface ISellerSubscriptionService
    {
        /// <summary>
        /// Create a new seller subscription
        /// </summary>
        Task<ApiResponse<Guid>> CreateSubscriptionAsync(string orgId, CreateSubscriptionRequest request, Guid createdBy);

        /// <summary>
        /// Authenticate seller with Amazon and save access tokens
        /// </summary>
        Task<ApiResponse<bool>> AuthenticateAndSaveSellerDataAsync(string orgId, AuthenticateSellerRequest request);

        /// <summary>
        /// Get all subscriptions for current organization
        /// </summary>
        Task<ApiResponse<List<SubscriptionListItem>>> GetAllSubscriptionsAsync(string orgId);

        /// <summary>
        /// Get detailed subscription information
        /// </summary>
        Task<ApiResponse<List<SubscriptionDetail>>> GetSubscriptionDetailsAsync(string orgId);

        /// <summary>
        /// Get subscription by ID
        /// </summary>
        Task<ApiResponse<SellerSubscription>> GetSubscriptionByIdAsync(string orgId, Guid subId);

        /// <summary>
        /// Get all countries for organization subscriptions
        /// </summary>
        Task<ApiResponse<List<string>>> GetAllCountriesAsync(string orgId);

        /// <summary>
        /// Get marketplace information by region
        /// </summary>
        Task<ApiResponse<List<RegionalMarketplace>>> GetMarketplacesByRegionAsync(string region);

        /// <summary>
        /// Check if subscription data already exists
        /// </summary>
        Task<bool> SubscriptionExistsAsync(string orgId, string sellingPartnerId, string mwsAuthToken);
    }
}
