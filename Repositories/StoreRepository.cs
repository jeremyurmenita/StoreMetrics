using MongoDB.Driver;
using StoreMetrics.Models;
using StoreMetrics.Services;
using StoreMetrics.ViewModels;

namespace StoreMetrics.Repositories
{
    public class StoreRepository : IStoreRepository
    {
        private readonly IMongoCollection<Store> _collection;
        private readonly IMongoCollection<StoreRating> _ratings;
        private readonly IMongoCollection<StorePerformance> _performances;
        private readonly FileEvaluationRepository _evalRepo;

        public StoreRepository(MongoDbService dbService, FileEvaluationRepository evalRepo)
        {
            _collection = dbService.StoreCollection;
            _ratings = dbService.StoreRatings;
            _performances = dbService.StorePerformances;
            _evalRepo = evalRepo;
        }

        public async Task<IEnumerable<StoreVm>> GetAllAsync()
        {
            var stores = await _collection.Find(_ => true).ToListAsync();

            return stores.Select(s => new StoreVm
            {
                Id = s.Id,
                StoreName = s.StoreName,
                BuildingNumber = s.BuildingNumber,
                StreetName = s.StreetName,
                Brgy = s.Brgy,
                City = s.City,
                Province = s.Province,
                PostalCode = s.PostalCode,
                AuditDate = s.AuditDate,
                IsActive = s.IsActive
            });
        }

        public async Task<StoreVm?> GetAsync(string id)
        {
            var s = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (s == null) return null;

            return new StoreVm
            {
                Id = s.Id,
                StoreName = s.StoreName,
                BuildingNumber = s.BuildingNumber,
                StreetName = s.StreetName,
                Brgy = s.Brgy,
                City = s.City,
                Province = s.Province,
                PostalCode = s.PostalCode,
                AuditDate = s.AuditDate,
                IsActive = s.IsActive
            };
        }

        public async Task CreateAsync(StoreVm vm)
        {
            // Normalize for comparison
            string name = vm.StoreName.Trim().ToLower();
            string building = (vm.BuildingNumber ?? "").Trim().ToLower();
            string street = vm.StreetName.Trim().ToLower();
            string brgy = vm.Brgy.Trim().ToLower();
            string city = vm.City.Trim().ToLower();
            string province = vm.Province.Trim().ToLower();
            string postal = vm.PostalCode.Trim().ToLower();

            // Check for EXACT DUPLICATE (same name + same full address)
            var duplicate = await _collection.Find(s =>
                s.StoreName.ToLower() == name &&
                (s.BuildingNumber ?? "").ToLower() == building &&
                s.StreetName.ToLower() == street &&
                s.Brgy.ToLower() == brgy &&
                s.City.ToLower() == city &&
                s.Province.ToLower() == province &&
                s.PostalCode.ToLower() == postal
            ).FirstOrDefaultAsync();

            if (duplicate != null)
                throw new InvalidOperationException("This store already exists at the same exact location.");

            // Otherwise allow — same store name but different location is OK
            var store = new Store
            {
                StoreName = vm.StoreName,
                BuildingNumber = vm.BuildingNumber,
                StreetName = vm.StreetName,
                Brgy = vm.Brgy,
                City = vm.City,
                Province = vm.Province,
                PostalCode = vm.PostalCode,
                AuditDate = vm.AuditDate!.Value,
                IsActive = true
            };

            await _collection.InsertOneAsync(store);
        }

        public async Task UpdateAsync(StoreVm vm)
        {
            var duplicate = await _collection.Find(s =>
                s.Id != vm.Id && // exclude current store
                s.StoreName.ToLower() == vm.StoreName.ToLower() &&
                (s.BuildingNumber ?? "") == (vm.BuildingNumber ?? "") &&
                s.StreetName.ToLower() == vm.StreetName.ToLower() &&
                s.Brgy.ToLower() == vm.Brgy.ToLower() &&
                s.City.ToLower() == vm.City.ToLower() &&
                s.Province.ToLower() == vm.Province.ToLower() &&
                s.PostalCode == vm.PostalCode
            ).FirstOrDefaultAsync();

            if (duplicate != null)
                throw new InvalidOperationException("Another store already exists with the same name and address.");

            var update = Builders<Store>.Update
                .Set(s => s.StoreName, vm.StoreName)
                .Set(s => s.BuildingNumber, vm.BuildingNumber)
                .Set(s => s.StreetName, vm.StreetName)
                .Set(s => s.Brgy, vm.Brgy)
                .Set(s => s.City, vm.City)
                .Set(s => s.Province, vm.Province)
                .Set(s => s.PostalCode, vm.PostalCode)
                .Set(s => s.AuditDate, vm.AuditDate!.Value)
                .Set(s => s.IsActive, vm.IsActive);

            await _collection.UpdateOneAsync(s => s.Id == vm.Id, update);

            // Sync other collections
            await _ratings.UpdateManyAsync(r => r.StoreId == vm.Id,
                Builders<StoreRating>.Update.Set(r => r.StoreName, vm.StoreName));

            await _performances.UpdateManyAsync(p => p.StoreId == vm.Id,
                Builders<StorePerformance>.Update.Set(p => p.StoreName, vm.StoreName));

            await _evalRepo.UpdateStoreNameAsync(vm.Id!, vm.StoreName);
        }

        public async Task DeleteAsync(string id)
        {
            await _collection.DeleteOneAsync(s => s.Id == id);
            await _ratings.DeleteManyAsync(r => r.StoreId == id);
            await _performances.DeleteManyAsync(p => p.StoreId == id);
        }

        public async Task ToggleStatusAsync(string id)
        {
            var store = await _collection.Find(s => s.Id == id).FirstOrDefaultAsync();
            if (store == null) return;

            await _collection.UpdateOneAsync(
                s => s.Id == id,
                Builders<Store>.Update.Set(s => s.IsActive, !store.IsActive)
            );
        }
    }
}
