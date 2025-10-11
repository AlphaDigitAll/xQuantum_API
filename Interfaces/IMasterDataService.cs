using xQuantum_API.Models.Common;

namespace xQuantum_API.Interfaces
{
    public interface IMasterDataService
    {
        Task<IEnumerable<DropdownItem>> GetBusinessTypesAsync(string searchTerm);
        Task<IEnumerable<DropdownItem>> GetCitiesAsync(string countryId, string searchTerm);
        Task<IEnumerable<DropdownItem>> GetCountriesAsync(string searchTerm);
        Task<IEnumerable<DropdownItem>> GetMobileCountriesCodeAsync(string searchTerm);
        Task<IEnumerable<DropdownItem>> GetPlansAsync(string searchTerm);
    }
}
