using Microsoft.EntityFrameworkCore;

namespace pax.dsstats.dbng;

[Keyless]
public class GroupByHelper
{
    public bool Group { get; set; }
    public int Count { get; set; }
}
