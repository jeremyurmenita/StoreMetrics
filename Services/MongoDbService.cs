using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StoreMetrics.Models;

namespace StoreMetrics.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Store> _stores;

        private readonly IMongoCollection<StoreRating> _storeRatings;
        private readonly IMongoCollection<StorePerformance> _storePerformances;

        public MongoDbService(IOptions<MongoSettings> settings)
        {
            var mongoClient = new MongoClient(settings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);

            _users = mongoDatabase.GetCollection<User>(settings.Value.UsersCollection);
            _stores = mongoDatabase.GetCollection<Store>(settings.Value.StoresCollection);

            _storeRatings = mongoDatabase.GetCollection<StoreRating>("store_ratings");
            _storePerformances = mongoDatabase.GetCollection<StorePerformance>("store_performances");
        }

        // ---------- USERS ----------
        public async Task CreateUserAsync(User user) =>
            await _users.InsertOneAsync(user);

        public async Task<User?> GetUserByUsernameAsync(string username) =>
            await _users.Find(x => x.Username == username).FirstOrDefaultAsync();

        public async Task<User?> GetUserByIdAsync(string id) =>
            await _users.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<User?> GetUserByEmailAsync(string email) =>
            await _users.Find(x => x.Email == email).FirstOrDefaultAsync();

        public async Task UpdateUserProfileAsync(string id, string firstName, string? middleName, string lastName, string email, string phoneNumber)
        {
            var update = Builders<User>.Update
                .Set(u => u.FirstName, firstName)
                .Set(u => u.MiddleName, middleName)
                .Set(u => u.LastName, lastName)
                .Set(u => u.Email, email)
                .Set(u => u.PhoneNumber, phoneNumber);

            await _users.UpdateOneAsync(u => u.Id == id, update);
        }

        public async Task UpdateUserPasswordAsync(string id, string newPassword)
        {
            await _users.UpdateOneAsync(
                u => u.Id == id,
                Builders<User>.Update.Set(u => u.Password, newPassword)
            );
        }

        // ---------- STORES ----------
        public IMongoCollection<Store> StoreCollection => _stores;

        public IMongoCollection<StoreRating> StoreRatings => _storeRatings;
        public IMongoCollection<StorePerformance> StorePerformances => _storePerformances;
    }

    public class MongoSettings
    {
        public string ConnectionString { get; set; } = "";
        public string DatabaseName { get; set; } = "";
        public string UsersCollection { get; set; } = "";
        public string StoresCollection { get; set; } = "";
    }
}
