using MongoDB.Driver;
using GOCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;


namespace Services
{
    public class UserMongoDBService : IUserDBService
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly ILogger<UserMongoDBService> _logger;

        public UserMongoDBService(ILogger<UserMongoDBService> logger, IConfiguration configuration)
        {
            _logger = logger;

            var connectionString = configuration.GetConnectionString("MongoDb");
            var client = new MongoClient(connectionString);

            var database = client.GetDatabase("GO-UserServiceDB");

            _userCollection = database.GetCollection<User>("Users");
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                user.Id = Guid.NewGuid(); // Sørg for en valid GUID
                await _userCollection.InsertOneAsync(user);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fejl ved oprettelse af bruger i MongoDB.");
                throw;
            }
        }



        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            var user = await _userCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
            return user;
        }

        public async Task<User> GetUserByNameAsync(string name)
        {
            var user = await _userCollection.Find(u => u.Name == name).FirstOrDefaultAsync();
            return user;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userCollection.Find(u => true).ToListAsync();
        }

        public async Task<User> UpdateUserAsync(string id, User updatedUser)
        {
            var result = await _userCollection.ReplaceOneAsync(u => u.Id == Guid.Parse(id), updatedUser);
            return updatedUser;
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var result = await _userCollection.DeleteOneAsync(u => u.Id == Guid.Parse(id));
            return result.DeletedCount > 0;
        }
    }
}
