using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8;

public class DsPickBan
{
    public int DsPickBanId { get; set; }
    public PickBanMode PickBanMode { get; set; }
    [Precision(0)]
    public DateTime Time {  get; set; }
    public List<Commander> Bans { get; set; } = [];
    public List<Commander> Picks { get; set; } = [];
}
