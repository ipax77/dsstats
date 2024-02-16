using System.ComponentModel.DataAnnotations;

namespace dsstats.shared;

public record FaqDto
{
    public int FaqId { get; set; }
    [MaxLength(100)]
    public string Question { get; set; } = string.Empty;
    [MaxLength(400)]
    public string Answer { get; set; } = string.Empty;
    public FaqLevel Level { get; set; }
    public int Upvotes { get; set; }
}

public record FaqListDto
{
    public int FaqId { get; set; }
    [MaxLength(100)]
    public string Question { get; set; } = string.Empty;
    public FaqLevel Level { get; set; }
    public int Upvotes { get; set; }
}

public record FaqRequest
{
    public string Search { get; set; } = string.Empty;
    public FaqLevel Level { get; set; }
    public List<TableOrder> Orders { get; set; } = new();
    public int Skip { get; set; }
    public int Take { get; set; }
}

