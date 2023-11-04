using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class BattleNetInfo
{
    public int BattleNetInfoId { get; set; }

    public int BattleNetId { get; set; }

    public int UploaderId { get; set; }

    public virtual Uploader Uploader { get; set; } = null!;
}
