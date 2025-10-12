using Microsoft.Extensions.Configuration;
using xQuantum_API.Infrastructure;
using xQuantum_API.Interfaces.Customers;
using xQuantum_API.Interfaces.Tenant;
using xQuantum_API.Models.Customers;

namespace xQuantum_API.Services.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly ILogger<CustomerService> _logger;
        private readonly ITenantService tenantService;
        public CustomerService(ILogger<CustomerService> logger, ITenantService _tenantService)
        {
            _logger = logger;
            _tenantService = tenantService;
        }




    }
}
