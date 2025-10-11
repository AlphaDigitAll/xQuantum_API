using xQuantum_API.Infrastructure;
using xQuantum_API.Interfaces;
using xQuantum_API.Interfaces.Reports;
using xQuantum_API.Repositories;
using xQuantum_API.Services;
using xQuantum_API.Services.Reports;

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
            // Add these lines in your service registration
            services.AddHttpContextAccessor(); // ← Required!

        }
    }
}
