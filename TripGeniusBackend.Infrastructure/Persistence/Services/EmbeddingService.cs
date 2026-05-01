using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Settings;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public EmbeddingService(HttpClient httpClient, IOptions<OpenRouterSettings> openRouterSettings)
    {
        _httpClient = httpClient;
        _apiKey = openRouterSettings.Value.ApiKey;
    }

    public async Task<float[]> GetEmbedding(string text)
    {
        var body = new
        {
            model = "nvidia/llama-nemotron-embed-vl-1b-v2:free",
            input = text
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }

}