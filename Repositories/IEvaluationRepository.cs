using StoreMetrics.ViewModels;

namespace StoreMetrics.Repositories
{
    public interface IEvaluationRepository
    {
        Task CreateAsync(EvaluationVm e);
        Task<IEnumerable<EvaluationVm>> GetAllAsync();
        Task<IEnumerable<EvaluationVm>> GetByStoreAsync(string storeId);
        Task<EvaluationVm?> GetAsync(string id);
        Task UpdateAsync(EvaluationVm e);         // <-- NEW
        Task DeleteAsync(string id);
    }
}
