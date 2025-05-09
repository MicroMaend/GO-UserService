using MongoDB.Driver;
using GOCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public class UserMongoDBService : IUserDBService
    {
        private readonly IMongoCollection<Customer> _customerCollection;
        private readonly ILogger<UserMongoDBService> _logger;

        // Konstruktør med korrekt parameter og lukning af parentes
        public UserMongoDBService(ILogger<UserMongoDBService> logger, IConfiguration configuration)
        {
            _logger = logger;

            // Hent konfigurationen fra appsettings eller miljøvariabler
            var connectionString = configuration["MongoConnectionString"] ?? "<blank>";
            var databaseName = configuration["DatabaseName"] ?? "<blank>";
            var collectionName = configuration["CollectionName"] ?? "<blank>";

            _logger.LogInformation($"Connected to MongoDB using: {connectionString}");
            _logger.LogInformation($"Using database: {databaseName}");
            _logger.LogInformation($"Using Collection: {collectionName}");

            try
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                _customerCollection = database.GetCollection<Customer>(collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to connect to MongoDB: {0}", ex.Message);
            }
        }

        // Opret bruger i MongoDB
        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            // TODO: Validering af user og fejlhåndtering
            await _customerCollection.InsertOneAsync(customer);
            return customer;
        }

        // Hent bruger baseret på ID
        public async Task<Customer> GetCustomerByIdAsync(string id)
        {
            var customer = await _customerCollection.Find(u => u.Id.ToString() == id).FirstOrDefaultAsync();
            return customer;
        }

        // Hent alle brugere
        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            var customers = await _customerCollection.Find(u => true).ToListAsync();
            return customers;
        }

        // Opdater bruger
        public async Task<Customer> UpdateCustomerAsync(string id, Customer updatedCustomer)
        {
            var result = await _customerCollection.ReplaceOneAsync(u => u.Id.ToString() == id, updatedCustomer);
            return updatedCustomer;
        }

        // Slet bruger
        public async Task<bool> DeleteCustomerAsync(string id)
        {
            var result = await _customerCollection.DeleteOneAsync(u => u.Id.ToString() == id);
            return result.DeletedCount > 0;
        }
    }
}