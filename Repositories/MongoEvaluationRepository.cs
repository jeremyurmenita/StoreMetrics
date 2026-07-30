using MongoDB.Driver;
using StoreMetrics.Models;
using StoreMetrics.Services;
using StoreMetrics.ViewModels;

namespace StoreMetrics.Repositories
{
    public class MongoEvaluationRepository : IEvaluationRepository
    {
        private readonly MongoDbService _db;
        private readonly IMongoCollection<StoreRating> _ratings;
        private readonly IMongoCollection<StorePerformance> _performances;

        public MongoEvaluationRepository(MongoDbService db)
        {
            _db = db;
            _ratings = db.StoreRatings;
            _performances = db.StorePerformances;
        }

        // ✅ Create evaluation + summary
        public async Task CreateAsync(EvaluationVm e)
        {
            // Store detailed rating
            var rating = new StoreRating
            {
                StoreId = e.StoreId,
                StoreName = e.StoreName,
                Cleanliness = e.Cleanliness,
                Condition = e.Condition,
                CustomerEngagement = e.CustomerEngagement,
                PersonalGrooming = e.PersonalGrooming,
                Accuracy = e.Accuracy,
                SpeedOfService = e.SpeedOfService,
                ProductQuality = e.ProductQuality,
                Remarks = e.Remarks,
                EvaluationDate = DateTime.Today
            };
            await _ratings.InsertOneAsync(rating);

            // Store performance summary
            var performance = new StorePerformance
            {
                StoreId = e.StoreId,
                StoreName = e.StoreName,
                AverageRating = e.AverageRating,
                PerformancePercent = e.PerformancePercent,
                PerformanceDescription = e.PerformanceDescription,
                EvaluationDate = DateTime.Today
            };
            await _performances.InsertOneAsync(performance);
        }

        // The rest required by interface (dummy, since you use FileEvaluationRepository for viewing)
        public Task<IEnumerable<EvaluationVm>> GetAllAsync() => Task.FromResult(Enumerable.Empty<EvaluationVm>());
        public Task<IEnumerable<EvaluationVm>> GetByStoreAsync(string storeId) => Task.FromResult(Enumerable.Empty<EvaluationVm>());
        public Task<EvaluationVm?> GetAsync(string id) => Task.FromResult<EvaluationVm?>(null);
        public Task UpdateAsync(EvaluationVm e) => Task.CompletedTask;
        public Task DeleteAsync(string id) => Task.CompletedTask;
    }
}
