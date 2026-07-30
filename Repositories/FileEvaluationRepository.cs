using System.Text.Json;
using StoreMetrics.ViewModels;

namespace StoreMetrics.Repositories
{
    public class FileEvaluationRepository : IEvaluationRepository
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        private readonly SemaphoreSlim _gate = new(1, 1);

        public FileEvaluationRepository(IWebHostEnvironment env)
        {
            var dataDir = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDir);

            _filePath = Path.Combine(dataDir, "evaluations.json");

            // ✅ Ensure file exists and is initialized
            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "[]");
            }
            else if (new FileInfo(_filePath).Length == 0)
            {
                // Empty file fix
                File.WriteAllText(_filePath, "[]");
            }
        }

        // ✅ Safe loader with JSON validation and auto-fix for bad data
        private async Task<List<EvaluationVm>> LoadAsync()
        {
            try
            {
                using var s = File.OpenRead(_filePath);
                var items = await JsonSerializer.DeserializeAsync<List<EvaluationVm>>(s, _json);
                return items ?? new();
            }
            catch (JsonException)
            {
                // File corrupted or invalid JSON — reset
                File.WriteAllText(_filePath, "[]");
                return new();
            }
            catch (Exception)
            {
                // Any other IO issue
                return new();
            }
        }

        private async Task SaveAsync(List<EvaluationVm> items)
        {
            using var s = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(s, items, _json);
        }

        public async Task CreateAsync(EvaluationVm e)
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                e.Id ??= Guid.NewGuid().ToString();
                items.Add(e);
                await SaveAsync(items);
            }
            finally { _gate.Release(); }
        }

        public async Task<IEnumerable<EvaluationVm>> GetAllAsync()
        {
            await _gate.WaitAsync();
            try { return await LoadAsync(); }
            finally { _gate.Release(); }
        }

        public async Task<IEnumerable<EvaluationVm>> GetByStoreAsync(string storeId)
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                return items.Where(x => x.StoreId == storeId);
            }
            finally { _gate.Release(); }
        }

        public async Task<EvaluationVm?> GetAsync(string id)
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                return items.FirstOrDefault(x => x.Id == id);
            }
            finally { _gate.Release(); }
        }

        public async Task UpdateAsync(EvaluationVm e)
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                var i = items.FindIndex(x => x.Id == e.Id);
                if (i >= 0) items[i] = e;
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
        // ✅ Sync store name across all evaluations for that store
        public async Task UpdateStoreNameAsync(string storeId, string newName)
        {
            await _gate.WaitAsync();
            try
            {
                var items = await LoadAsync();
                var updated = false;

                foreach (var eval in items.Where(e => e.StoreId == storeId))
                {
                    if (eval.StoreName != newName)
                    {
                        eval.StoreName = newName;
                        updated = true;
                    }
                }

                if (updated)
                    await SaveAsync(items);
            }
            finally { _gate.Release(); }
        }

    }
}
