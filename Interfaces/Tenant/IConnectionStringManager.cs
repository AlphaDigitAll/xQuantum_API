namespace xQuantum_API.Interfaces.Tenant
{
    public interface IConnectionStringManager
    {
        Task<string> GetOrAddConnectionStringAsync(string orgId, Func<Task<string>> connectionStringFactory);
        void RemoveConnectionString(string orgId);
        void ClearAll();
    }
}
