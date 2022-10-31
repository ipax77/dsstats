using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace sc2dsstats.shared
{
    public class DsBuildRequest : DsRequest
    {
        public string Playername { get; set; } = "PAX";
        public List<string> Playernames { get; set; } = new();
        [NotMapped]
        [JsonIgnore]
        public string CacheKey => $"Build{Interest}{(String.IsNullOrEmpty(Versus) ? "" : Versus)}{Timespan?.Replace(" ", "")}{(String.IsNullOrEmpty(Playername) ? "" : Playername)}";
    }
}
