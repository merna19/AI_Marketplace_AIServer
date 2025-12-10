using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Embeddings.Services
{
    public class ChromaService
    {
        private readonly HttpClient _httpClient;
        private readonly EmbeddingService _embeddingService;
        private const string CollectionName = "products";

        // 1. UPDATE THESE CONSTANTS FROM YOUR SNIPPET
        private const string Tenant = "53a7f7fc-807d-45d7-85e3-2f09b5f3b44d"; // Your Tenant ID
        private const string Database = "Project"; // Your Database Name

        private string? _collectionId;

        public ChromaService(EmbeddingService embeddingService)
        {
            _embeddingService = embeddingService;

            // 2. USE THE CLOUD URL AND API KEY
            string cloudUrl = "https://api.trychroma.com/";
            string apiKey = "ck-GVsjzDsKSyo12z8mZG4V4UoqT4UnRVY3ZonLnkJwFdf7";

            var handler = new HttpClientHandler();

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(cloudUrl),
                Timeout = TimeSpan.FromMinutes(2)
            };

            // 3. SET THE AUTH HEADER
            // Chroma Cloud uses "X-Chroma-Token" for API keys starting with 'ck-'
            _httpClient.DefaultRequestHeaders.Add("X-Chroma-Token", apiKey);
        }

        public async Task InitializeAsync()
        {
            try
            {
                Console.WriteLine($"Initializing ChromaDB v0.6+ connection...");

                // 1. List Collections (using the new nested path)
                // GET /api/v2/tenants/{tenant}/databases/{database}/collections
                var listEndpoint = $"api/v2/tenants/{Tenant}/databases/{Database}/collections";
                var listResponse = await _httpClient.GetAsync(listEndpoint);

                if (listResponse.IsSuccessStatusCode)
                {
                    var content = await listResponse.Content.ReadAsStringAsync();
                    var collections = JsonSerializer.Deserialize<List<CollectionInfo>>(content);

                    var existing = collections?.FirstOrDefault(c => c.Name == CollectionName);

                    if (existing != null)
                    {
                        _collectionId = existing.Id;
                        Console.WriteLine($"✓ Collection '{CollectionName}' found (ID: {_collectionId})");
                        return;
                    }
                }

                // 2. Create Collection
                Console.WriteLine($"Collection not found, creating new one...");

                var createRequest = new
                {
                    name = CollectionName,
                    metadata = new Dictionary<string, string>
                    {
                        ["description"] = "Product embeddings with category information",
                        ["hnsw:space"] = "cosine"
                    }
                };

                var json = JsonSerializer.Serialize(createRequest);
                var contentReq = new StringContent(json, Encoding.UTF8, "application/json");

                // POST /api/v2/tenants/{tenant}/databases/{database}/collections
                var createEndpoint = $"api/v2/tenants/{Tenant}/databases/{Database}/collections";
                var createResponse = await _httpClient.PostAsync(createEndpoint, contentReq);
                var createResult = await createResponse.Content.ReadAsStringAsync();

                if (createResponse.IsSuccessStatusCode)
                {
                    var collection = JsonSerializer.Deserialize<CollectionInfo>(createResult);
                    _collectionId = collection?.Id;
                    Console.WriteLine($"✓ Collection created (ID: {_collectionId})");
                }
                else
                {
                    throw new InvalidOperationException($"Failed to create collection: {createResult}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Init Error: {ex.Message}");
                throw;
            }
        }

        public async Task IndexProductsAsync(IEnumerable<Product> products)
        {
            var productList = products.ToList();
            if (productList.Count == 0) return;

            if (string.IsNullOrEmpty(_collectionId)) await InitializeAsync();

            var ids = new List<string>();
            var embeddings = new List<double[]>();
            var metadatas = new List<Dictionary<string, object>>();
            var documents = new List<string>();

            foreach (var product in productList)
            {
                var combinedText = $"{product.Name} {product.Description} {product.Category}";
                var embedding = _embeddingService.GenerateEmbedding(combinedText);

                ids.Add(product.Id.ToString());
                embeddings.Add(embedding.Select(f => (double)f).ToArray());
                metadatas.Add(new Dictionary<string, object>
                {
                    ["product_id"] = product.Id,
                    ["name"] = product.Name ?? string.Empty,
                    ["description"] = product.Description ?? string.Empty,
                    ["category"] = product.Category ?? string.Empty
                });
                documents.Add(combinedText);
            }

            var request = new
            {
                ids = ids.ToArray(),
                embeddings = embeddings.ToArray(),
                metadatas = metadatas.ToArray(),
                documents = documents.ToArray()
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // POST /api/v2/tenants/{tenant}/databases/{database}/collections/{id}/add
            var addEndpoint = $"api/v2/tenants/{Tenant}/databases/{Database}/collections/{_collectionId}/add";
            var response = await _httpClient.PostAsync(addEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Batch index failed: {error}");
            }

            Console.WriteLine($"✓ Indexed {productList.Count} products with categories");
        }

        public async Task<List<SearchResult>> SearchAsync(string query, int topK = 5)
        {
            if (string.IsNullOrEmpty(_collectionId)) await InitializeAsync();

            var queryEmbedding = _embeddingService.GenerateEmbedding(query);

            var request = new
            {
                query_embeddings = new[] { queryEmbedding.Select(f => (double)f).ToArray() },
                n_results = topK
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // POST /api/v2/tenants/{tenant}/databases/{database}/collections/{id}/query
            var queryEndpoint = $"api/v2/tenants/{Tenant}/databases/{Database}/collections/{_collectionId}/query";
            var response = await _httpClient.PostAsync(queryEndpoint, content);

            if (!response.IsSuccessStatusCode) return new List<SearchResult>();

            var resultJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<QueryResponse>(resultJson);

            var searchResults = new List<SearchResult>();

            if (result?.Ids != null && result.Ids.Count > 0 && result.Ids[0].Count > 0)
            {
                for (int i = 0; i < result.Ids[0].Count; i++)
                {
                    var metadata = result.Metadatas?[0][i];
                    var distance = result.Distances?[0][i] ?? 0;

                    if (metadata != null)
                    {
                        searchResults.Add(new SearchResult
                        {
                            ProductId = GetIntFromMetadata(metadata, "product_id"),
                            Name = GetStringFromMetadata(metadata, "name"),
                            Description = GetStringFromMetadata(metadata, "description"),
                            Category = GetStringFromMetadata(metadata, "category"),
                            Score = 1.0 - distance
                        });
                    }
                }
            }

            return searchResults;
        }

        // Single product helper
        public async Task IndexProductAsync(int productId, string name, string description, string category)
        {
            var p = new Product { Id = productId, Name = name, Description = description, Category = category };
            await IndexProductsAsync(new[] { p });
        }

        public async Task ClearAllAsync()
        {
            Console.WriteLine($"Clearing collection '{CollectionName}'...");

            var deleteEndpoint = $"api/v2/tenants/{Tenant}/databases/{Database}/collections/{CollectionName}";
            var response = await _httpClient.DeleteAsync(deleteEndpoint);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Warning: Could not delete collection: {error}");
            }
            else
            {
                Console.WriteLine($"✓ Collection '{CollectionName}' deleted successfully (or didn't exist)");
            }

            _collectionId = null;
            await InitializeAsync();
        }

        private int GetIntFromMetadata(Dictionary<string, object> metadata, string key)
        {
            if (!metadata.ContainsKey(key)) return 0;
            var value = metadata[key];
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number) 
                return element.GetInt32();
            return Convert.ToInt32(value);
        }

        private string GetStringFromMetadata(Dictionary<string, object> metadata, string key)
        {
            if (!metadata.ContainsKey(key)) return string.Empty;
            var value = metadata[key];
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? string.Empty;
            return value?.ToString() ?? string.Empty;
        }

        // --- Models ---
        private class CollectionInfo
        {
            [JsonPropertyName("id")] public string? Id { get; set; }
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("metadata")] public Dictionary<string, object>? Metadata { get; set; }
        }

        private class QueryResponse
        {
            [JsonPropertyName("ids")] public List<List<string>>? Ids { get; set; }
            [JsonPropertyName("distances")] public List<List<double>>? Distances { get; set; }
            [JsonPropertyName("metadatas")] public List<List<Dictionary<string, object>>>? Metadatas { get; set; }
        }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class SearchResult
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}