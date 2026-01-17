using Microsoft.Azure.Cosmos;
using SmartFeedbackCollector.Models.Configuration;
using SmartFeedbackCollector.Models.Domain;
using SmartFeedbackCollector.Repositories.Interfaces;
using SmartFeedbackCollector.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace SmartFeedbackCollector.Repositories
{

    public class CosmosDbFeedbackRepository : IFeedbackRepository
    {
        private readonly IConfigurationService _configService;
        private Container? _container;
        private CosmosClient? _client;
        private bool _initializationAttempted = false;
        private string? _initializationError;

        public CosmosDbFeedbackRepository(IConfigurationService configService)
        {
            _configService = configService;
        }

        private async Task<Container?> GetContainerAsync()
        {
            if (_initializationAttempted && _container != null)
                return _container;

            if (_initializationAttempted && _initializationError != null)
                throw new InvalidOperationException($"CosmosDbFeedbackRepository failed to initialize: {_initializationError}");

            _initializationAttempted = true;

            try
            {
                var options = await _configService.GetConfigurationAsync<AzureStorageOptions>("AzureStorage");
                
                var clientOptions = new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    RequestTimeout = System.TimeSpan.FromSeconds(10),
                    MaxRetryAttemptsOnRateLimitedRequests = 3,
                    MaxRetryWaitTimeOnRateLimitedRequests = System.TimeSpan.FromSeconds(5)
                };

                _client = new CosmosClient(options.ConnectionString, clientOptions);
                var database = _client.GetDatabase(options.DatabaseName);
                _container = database.GetContainer(options.ContainerName);
                
                return _container;
            }
            catch (Exception ex)
            {
                _initializationError = ex.Message;
                Console.WriteLine($"Failed to initialize CosmosDbFeedbackRepository: {ex.Message}");
                throw new InvalidOperationException("Failed to initialize CosmosDbFeedbackRepository.", ex);
            }
        }

        /// <summary>
        /// Dodaje nowy feedback do bazy danych
        /// </summary>
        /// <param name="feedback">Obiekt feedback do zapisania</param>
        /// <returns>Zapisany feedback z wygenerowanymi metadanymi</returns>
        public async Task<Feedback> AddFeedbackAsync(Feedback feedback)
        {
            try
            {
                var response = await _container.CreateItemAsync(feedback, new PartitionKey(feedback.Id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // Obsługa ograniczenia prędkości (429 Too Many Requests)
                await Task.Delay(1000); // Czekaj 1 sekundę
                var response = await _container.CreateItemAsync(feedback, new PartitionKey(feedback.Id));
                return response.Resource;
            }
        }

        /// <summary>
        /// Pobiera wszystkie feedback'i z bazy danych, posortowane według czasu dodania (najnowsze pierwsze)
        /// </summary>
        /// <returns>Lista wszystkich feedback'ów</returns>
        public async Task<List<Feedback>> GetAllFeedbackAsync()
        {
            var container = await GetContainerAsync();
            if (container == null)
            {
                throw new InvalidOperationException($"Could not initialize database connection: {_initializationError}");
            }

            var queryDefinition = new QueryDefinition("SELECT * FROM c ORDER BY c.SubmissionTime DESC");
            var query = container.GetItemQueryIterator<Feedback>(queryDefinition,
                requestOptions: new QueryRequestOptions { MaxItemCount = 100 }); // Maksymalnie 100 elementów na stronę

            var results = new List<Feedback>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        /// <summary>
        /// Pobiera konkretny feedback na podstawie ID
        /// </summary>
        /// <param name="id">ID feedback'u do pobrania</param>
        /// <returns>Feedback o podanym ID lub null jeśli nie znaleziono</returns>
        public async Task<Feedback> GetFeedbackByIdAsync(string id)
        {
            var container = await GetContainerAsync();
            if (container == null)
            {
                throw new InvalidOperationException($"Could not initialize database connection: {_initializationError}");
            }

            try
            {
                var response = await container.ReadItemAsync<Feedback>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Aktualizuje istniejący feedback w bazie danych
        /// </summary>
        /// <param name="feedback">Zaktualizowany obiekt feedback</param>
        /// <returns>Zaktualizowany feedback</returns>
        public async Task<Feedback> UpdateFeedbackAsync(Feedback feedback)
        {
            var container = await GetContainerAsync();
            if (container == null)
            {
                throw new InvalidOperationException($"Could not initialize database connection: {_initializationError}");
            }

            try
            {
                var response = await container.ReplaceItemAsync(feedback, feedback.Id, new PartitionKey(feedback.Id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(1000);
                var response = await container.ReplaceItemAsync(feedback, feedback.Id, new PartitionKey(feedback.Id));
                return response.Resource;
            }
        }
    }
}