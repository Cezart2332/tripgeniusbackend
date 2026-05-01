namespace TripGeniusBackend.Application.Interfaces;

public interface IEmbeddingService
{
    public Task<float[]> GetEmbedding(string text);
}