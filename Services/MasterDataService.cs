using Npgsql;
using xQuantum_API.Interfaces;
using xQuantum_API.Models.Common;

namespace xQuantum_API.Services
{
    public class MasterDataService : IMasterDataService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MasterDataService> _logger;
        private readonly string _masterConnectionString;

        public MasterDataService(IConfiguration configuration, ILogger<MasterDataService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _masterConnectionString = _configuration.GetConnectionString("MasterDatabase");
        }

        private async Task<IEnumerable<DropdownItem>> GetDropdownAsync(string query, NpgsqlParameter[] parameters)
        {
            var list = new List<DropdownItem>();

            await using var conn = new NpgsqlConnection(_masterConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DropdownItem
                {
                    Id = reader[0].ToString(),
                    Name = reader[1].ToString()
                });
            }

            return list;
        }

        public Task<IEnumerable<DropdownItem>> GetBusinessTypesAsync(string searchTerm)
        {
            const string sql = @"
            SELECT type_id, type_name 
            FROM admin.business_master
            WHERE is_active = TRUE AND type_name ILIKE @search
            ORDER BY type_name
            LIMIT 50;";

            var param = new NpgsqlParameter[]
            {
            new NpgsqlParameter("search", $"%{searchTerm}%")
            };

            return GetDropdownAsync(sql, param);
        }

        public Task<IEnumerable<DropdownItem>> GetCitiesAsync(string countryId, string searchTerm)
        {
            const string sql = @"
        SELECT city_id, city_name
        FROM admin.city_master
        WHERE is_active = TRUE
          AND country_id = @countryId
          AND city_name ILIKE @search
        ORDER BY city_name
        LIMIT 50;";

            var parameters = new NpgsqlParameter[]
            {
        new NpgsqlParameter("countryId", countryId),
        new NpgsqlParameter("search", $"%{searchTerm}%")
            };

            return GetDropdownAsync(sql, parameters);
        }

        public Task<IEnumerable<DropdownItem>> GetCountriesAsync(string searchTerm)
        {
            const string sql = @"
            SELECT country_id, country_name
            FROM admin.country_master
            WHERE is_active = TRUE AND country_name ILIKE @search
            ORDER BY country_name
            LIMIT 50;";

            var param = new NpgsqlParameter[]
            {
            new NpgsqlParameter("search", $"%{searchTerm}%")
            };

            return GetDropdownAsync(sql, param);
        }

        public Task<IEnumerable<DropdownItem>> GetMobileCountriesCodeAsync(string searchTerm)
        {
            const string sql = @"
            SELECT alpha_3_code, telephone_code
            FROM admin.country_master
            WHERE is_active = TRUE AND country_name ILIKE @search
            ORDER BY country_name
            LIMIT 50;";

            var param = new NpgsqlParameter[]
            {
            new NpgsqlParameter("search", $"%{searchTerm}%")
            };

            return GetDropdownAsync(sql, param);
        }
        public Task<IEnumerable<DropdownItem>> GetPlansAsync(string searchTerm)
        {
            const string sql = @"
            SELECT plan_id, plan_name
            FROM admin.plan_master
            WHERE is_active = TRUE AND plan_name ILIKE @search
            ORDER BY plan_name
            LIMIT 50;";

            var param = new NpgsqlParameter[]
            {
            new NpgsqlParameter("search", $"%{searchTerm}%")
            };

            return GetDropdownAsync(sql, param);
        }
    }
}
