using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng;

public class DsUpdate
{
    public int DsUpdateId { get; set; }
    public Commander Commander { get; set; }
    [Precision(0)]
    public DateTime Time { get; set; }
    public string DiscordId { get; set; } = string.Empty;
    public string Change { get; set; } = string.Empty;
}
