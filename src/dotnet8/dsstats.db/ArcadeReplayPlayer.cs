using System;
using System.Collections.Generic;
using dsstats.shared;

namespace dsstats.db;

public partial class ArcadeReplayPlayer
{
    public int ArcadeReplayPlayerId { get; set; }

    public string Name { get; set; } = null!;

    public int SlotNumber { get; set; }

    public int Team { get; set; }

    public int Discriminator { get; set; }

    public PlayerResult PlayerResult { get; set; }

    public int ArcadePlayerId { get; set; }
    public virtual ArcadePlayer? ArcadePlayer { get; set; }

    public int ArcadeReplayId { get; set; }
    public virtual ArcadeReplay? ArcadeReplay { get; set; }
    public virtual ArcadeReplayPlayerRating? ArcadeReplayPlayerRating { get; set; }
}
