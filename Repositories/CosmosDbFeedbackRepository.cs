using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using SmartFeedbackCollector.Models.Configuration;
using SmartFeedbackCollector.Models.Domain;
using SmartFeedbackCollector.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace SmartFeedbackCollector.Repositories
{

    public class CosmosDbFeedbackRepository : IFeedbackRepository
    {
        private readonly Container _container;
        private readonly CosmosClient _client;

        /// <summary>
        /// Konstruktor - inicjalizuje połączenie z Cosmos DB
        /// </summary>
        /// <param name="options">Opcje konfiguracji Azure Storage</param>
        public CosmosDbFeedbackRepository(IOptions<AzureStorageOptions> options)
        {
            var clientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,           // Bezpośrednie połączenie (szybsze)
                RequestTimeout = System.TimeSpan.FromSeconds(10), // Timeout dla żądań
                MaxRetryAttemptsOnRateLimitedRequests = 3,        // Max próby przy ograniczeniu prędkości
                MaxRetryWaitTimeOnRateLimitedRequests = System.TimeSpan.FromSeconds(5) // Czas oczekiwania między próbami
            };

            _client = new CosmosClient(options.Value.ConnectionString, clientOptions);
            var database = _client.GetDatabase(options.Value.DatabaseName);
            _container = database.GetContainer(options.Value.ContainerName);
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
            var queryDefinition = new QueryDefinition("SELECT * FROM c ORDER BY c.SubmissionTime DESC");
            var query = _container.GetItemQueryIterator<Feedback>(queryDefinition,
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
            try
            {
                var response = await _container.ReadItemAsync<Feedback>(id, new PartitionKey(id));
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
            try
            {
                var response = await _container.ReplaceItemAsync(feedback, feedback.Id, new PartitionKey(feedback.Id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(1000);
                var response = await _container.ReplaceItemAsync(feedback, feedback.Id, new PartitionKey(feedback.Id));
                return response.Resource;
            }
        }
    }
}