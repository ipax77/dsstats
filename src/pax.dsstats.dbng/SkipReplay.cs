using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.dbng;

public class SkipReplay
{
    public int SkipReplayId { get; set; }
    [MaxLength(500)]
    public string Path { get; set; } = null!;
}
