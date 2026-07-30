using StoreMetrics.ViewModels;

namespace StoreMetrics.Repositories
{
    public interface IStoreRepository
    {
        Task<IEnumerable<StoreVm>> GetAllAsync();
        Task<StoreVm?> GetAsync(string id);
        Task CreateAsync(StoreVm s);
        Task UpdateAsync(StoreVm s);
        Task DeleteAsync(string id);

        // ✅ Add this new method for status toggling
        Task ToggleStatusAsync(string id);
    }
}
