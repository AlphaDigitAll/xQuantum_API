using Microsoft.Extensions.Configuration;
using xQuantum_API.Infrastructure;
using xQuantum_API.Interfaces;
using xQuantum_API.Models;
using xQuantum_API.Services;

namespace xQuantum_API.Repositories
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
