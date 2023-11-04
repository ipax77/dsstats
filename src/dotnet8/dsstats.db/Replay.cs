using System;
using System.Collections.Generic;
using dsstats.shared;

namespace dsstats.db;

public partial class Replay
{
    public Replay()
    {
        ReplayPlayers = new HashSet<ReplayPlayer>();
        Uploaders = new HashSet<Uploader>();
    }

    public int ReplayId { get; set; }

    public string FileName { get; set; } = null!;

    public bool TournamentEdition { get; set; }

    public DateTime GameTime { get; set; }

    public int Duration { get; set; }

    public int WinnerTeam { get; set; }

    public int PlayerResult { get; set; }

    public int GameMode { get; set; }

    public int Objective { get; set; }

    public int Bunker { get; set; }

    public int Cannon { get; set; }

    public int Minkillsum { get; set; }

    public int Maxkillsum { get; set; }

    public int Minarmy { get; set; }

    public int Minincome { get; set; }

    public int Maxleaver { get; set; }

    public byte Playercount { get; set; }

    public string ReplayHash { get; set; } = null!;

    public bool DefaultFilter { get; set; }

    public int Views { get; set; }

    public int Downloads { get; set; }

    public string Middle { get; set; } = null!;

    public string CommandersTeam1 { get; set; } = null!;

    public string CommandersTeam2 { get; set; } = null!;

    public int? ReplayEventId { get; set; }

    public bool ResultCorrected { get; set; }

    public int PlayerPos { get; set; }

    public bool Uploaded { get; set; }

    public DateTime? Imported { get; set; }
    public virtual ReplayEvent? ReplayEvent { get; set; }
    public virtual ReplayRating? ReplayRating { get; set; }
    public virtual ComboReplayRating? ComboReplayRating { get; set; }

    public virtual ICollection<ReplayPlayer> ReplayPlayers { get; set; }

    public virtual ICollection<Uploader> Uploaders { get; set; }
}
