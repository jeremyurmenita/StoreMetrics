using StoreMetrics.ViewModels;

namespace StoreMetrics.Repositories
{
    public class InMemoryStoreRepository : IStoreRepository
    {
        private readonly List<StoreVm> _items = new();

        // -------------------- GET ALL --------------------
        public Task<IEnumerable<StoreVm>> GetAllAsync()
            => Task.FromResult(_items.AsEnumerable());

        // -------------------- GET ONE --------------------
        public Task<StoreVm?> GetAsync(string id)
            => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        // -------------------- CREATE --------------------
        public Task CreateAsync(StoreVm s)
        {
            s.Id ??= Guid.NewGuid().ToString();
            s.IsActive = true; // ✅ Default to active when created
            _items.Add(s);
            return Task.CompletedTask;
        }

        // -------------------- UPDATE --------------------
        public Task UpdateAsync(StoreVm s)
        {
            var i = _items.FindIndex(x => x.Id == s.Id);
            if (i >= 0)
            {
                // ✅ Preserve active/inactive status during edits
                s.IsActive = _items[i].IsActive;
                _items[i] = s;
            }
            return Task.CompletedTask;
        }

        // -------------------- DELETE --------------------
        public Task DeleteAsync(string id)
        {
            _items.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }

        // -------------------- TOGGLE ACTIVE/INACTIVE --------------------
        public Task ToggleStatusAsync(string id)
        {
            var store = _items.FirstOrDefault(x => x.Id == id);
            if (store != null)
                store.IsActive = !store.IsActive; // flip status

            return Task.CompletedTask;
        }
    }
}
