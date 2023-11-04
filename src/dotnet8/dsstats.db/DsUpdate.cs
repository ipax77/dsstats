using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class DsUpdate
{
    public int DsUpdateId { get; set; }

    public int Commander { get; set; }

    public DateTime Time { get; set; }

    public string DiscordId { get; set; } = null!;

    public string Change { get; set; } = null!;
}
