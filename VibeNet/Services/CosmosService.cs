using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using VibeNet.Config;
using VibeNet.Models;

namespace VibeNet.Services
{
    public class CosmosService
    {
        public readonly CosmosClient _client;
        public readonly Database _database;
        public readonly Container _connnectionsContainer;
        public readonly Container _chatsContainer;

        public CosmosService(IOptions<AzureSettings> settings)
        {
            _client = new CosmosClient(settings.Value.CosmosDb);
            _database = _client.GetDatabase("vibenetdb");
            _connnectionsContainer = _database.GetContainer("connections");
            _chatsContainer = _database.GetContainer("chats");
        }
        public async Task TestConnectionAsync()
        {
            var dbResponse = await _database.ReadAsync();
            Console.WriteLine($"✅ Connected to CosmosDB : {dbResponse.Resource.Id}");
        }
        public async Task<int> GetConnectionCountAsync(string userId)
        {
            try
            {
                var response = await _connnectionsContainer.ReadItemAsync<UserConnections>(userId, new PartitionKey(userId));
                var doc = response.Resource;

                if (doc?.connections == null)
                    return 0;

                return doc.connections.Count(c => c.status == "accepted");
            }
            catch
            {
                return 0;
            }
        }
    }
}
