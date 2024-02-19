using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db8;

public class Faq
{
    public int FaqId { get; set; }
    [MaxLength(100)]
    public string Question { get; set; } = string.Empty;
    [MaxLength(400)]
    public string Answer {  get; set; } = string.Empty;
    public FaqLevel Level { get; set; }
    public int Upvotes { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Precision(0)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(20)]
    public string? CreatedBy { get; set; }
}

public class FaqVote
{
    public int FaqVoteId { get; set; }
    public int FaqId { get; set; }
}
