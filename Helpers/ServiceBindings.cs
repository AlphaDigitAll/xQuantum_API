using xQuantum_API.Infrastructure;
using xQuantum_API.Interfaces.Authentication;
using xQuantum_API.Interfaces.Customers;
using xQuantum_API.Interfaces.MasterData;
using xQuantum_API.Interfaces.Products;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Interfaces.Subscription;
using xQuantum_API.Interfaces.Tenant;
using xQuantum_API.Interfaces.UserRegistration;
using xQuantum_API.Services.Authentication;
using xQuantum_API.Services.Customers;
using xQuantum_API.Services.MasterData;
using xQuantum_API.Services.Products;
using xQuantum_API.Services.Reports;
using xQuantum_API.Services.Subscription;
using xQuantum_API.Services.Tenant;
using xQuantum_API.Services.UserRegistration;

namespace xQuantum_API.Helpers
{
    public static class ServiceBindings
    {
        public static void BindServices(this IServiceCollection services)
        {
            // Register services
            services.AddSingleton<IConnectionStringManager, ConnectionStringManager>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IMasterDataService, MasterDataService>();
            services.AddScoped<IUserRegistrationService, UserRegistrationService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<ISalesSummaryService, SalesSummaryService>();
            services.AddScoped<ISalesGraphService, SalesGraphService>();
            services.AddScoped<ISubProductColumnService, SubProductColumnService>();
            services.AddScoped<ISellerSubscriptionService, SellerSubscriptionService>();
            // Add these lines in your service registration
            services.AddHttpContextAccessor(); // ← Required!

        }
    }
}
