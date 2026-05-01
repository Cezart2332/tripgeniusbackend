using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Vector = Pgvector.Vector;

namespace TripGeniusBackend.Domain.Entities;

public class AiMemory
{
    public int Id { get; set; }
    public User User { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string MemoryType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Vector Embedding { get; set; }
}