using System.Text.Json;
using StoreMetrics.ViewModels;

namespace StoreMetrics.Repositories
{
    public class FileStoreRepository : IStoreRepository
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        private readonly SemaphoreSlim _gate = new(1, 1);

        public FileStoreRepository(IWebHostEnvironment env)
        {
            var dataDir = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDir);
            _filePath = Path.Combine(dataDir, "stores.json");

            // ✅ Initialize file if missing
            if (!File.Exists(_filePath))
                File.WriteAllText(_filePath, "[]");
        }

        // -------------------- Helpers --------------------
        private async Task<List<StoreVm>> LoadAsync()
        {
            using var s = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<List<StoreVm>>(s, _json) ?? new();
        }

        private async Task SaveAsync(List<StoreVm> items)
        {
            using var s = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(s, items, _json);
        }

        // -------------------- CRUD --------------------
        public async Task<IEnumerable<StoreVm>> GetAllAsync()
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                return items;
            }
            finally { _gate.Release(); }
        }

        public async Task<StoreVm?> GetAsync(string id)
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                return items.FirstOrDefault(x => x.Id == id);
            }
            finally { _gate.Release(); }
        }

        public async Task CreateAsync(StoreVm s)
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                s.Id ??= Guid.NewGuid().ToString();

                // ✅ Default to active when creating
                s.IsActive = true;

                items.Add(s);
                await SaveAsync(items);
            }
            finally { _gate.Release(); }
        }

        public async Task UpdateAsync(StoreVm s)
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                var i = items.FindIndex(x => x.Id == s.Id);
                if (i >= 0)
                {
                    s.IsActive = items[i].IsActive; // preserve active status
                    items[i] = s;
                }
                await SaveAsync(items);
            }
            finally { _gate.Release(); }
        }

        public async Task DeleteAsync(string id)
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                items.RemoveAll(x => x.Id == id);
                await SaveAsync(items);
            }
            finally { _gate.Release(); }
        }

        // -------------------- TOGGLE ACTIVE/INACTIVE --------------------
        public async Task ToggleStatusAsync(string id)
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                var store = items.FirstOrDefault(x => x.Id == id);
                if (store != null)
                {
                    store.IsActive = !store.IsActive;
                    await SaveAsync(items);
                }
            }
            finally { _gate.Release(); }
        }
    }
}
