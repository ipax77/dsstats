using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class Uploader
{
    public int UploaderId { get; set; }

    public Guid AppGuid { get; set; }

    public string AppVersion { get; set; } = null!;

    public string Identifier { get; set; } = null!;

    public DateTime LatestUpload { get; set; }

    public DateTime LatestReplay { get; set; }

    public int Games { get; set; }

    public int Wins { get; set; }

    public int Mvp { get; set; }

    public int MainCommander { get; set; }

    public int MainCount { get; set; }

    public int TeamGames { get; set; }

    public DateTime UploadLastDisabled { get; set; }

    public int UploadDisabledCount { get; set; }

    public bool UploadIsDisabled { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<BattleNetInfo> BattleNetInfos { get; set; } = new List<BattleNetInfo>();

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();

    public virtual ICollection<Replay> ReplaysReplays { get; set; } = new List<Replay>();
}
