using Microsoft.EntityFrameworkCore;

namespace dsstats.db8;

[Keyless]
public class GroupByHelper
{
    public bool Group { get; set; }
    public int Count { get; set; }
}
