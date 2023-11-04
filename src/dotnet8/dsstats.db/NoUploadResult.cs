using System;
using System.Collections.Generic;

namespace dsstats.db;

public partial class NoUploadResult
{
    public int NoUploadResultId { get; set; }

    public int TotalReplays { get; set; }

    public DateTime LatestReplay { get; set; }

    public int NoUploadTotal { get; set; }

    public int NoUploadDefeats { get; set; }

    public DateTime LatestNoUpload { get; set; }

    public DateTime LatestUpload { get; set; }

    public int PlayerId { get; set; }

    public DateTime Created { get; set; }

    public virtual Player Player { get; set; } = null!;
}
