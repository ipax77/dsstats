# Direct Strike Play Notes

This folder contains the experimental replay playback UI and shared playback model.
The current focus is the spawn playback canvas used from `/te/play`.

## Map Geometry Facts

- Direct Strike tracker map space is treated as `256 x 240` world units.
- The real battlefront separator is the diagonal line `x + y = 248`.
- The canvas grid should follow the rotated map axes, using constant `x + y` and `x - y` lines.
- Known spawn polygons from `Sc2DirectStrikeParser.TrackerLayout.cs`:
  - Team 1/top right: `(165,174) -> (182,157) -> (171,146) -> (154,163)`
  - Team 2/bottom left: `(84,93) -> (101,76) -> (90,65) -> (73,82)`
- Team 1 is the top-right side on the canvas; Team 2 is the bottom-left side.

## Playback Data Facts

- Parser unit `Gameloop` values are real unit-born timings. A single player spawn can take several gameloops, especially in late game.
- `DirectStrikePlayerSpawn.StartGameloop` is the first unit born in that player spawn.
- `DirectStrikePlayerSpawn.EndGameloop` is the last unit born in that player spawn.
- Middle control comes from `FirstMiddleControlTeam` and `MiddleChanges`.
  - The first `MiddleChanges` value is when `FirstMiddleControlTeam` takes mid.
  - Later values alternate control between teams.
- Objective landmarks can disappear when the source objective born event has a matching death gameloop.

## Newly Measured Timing Facts

Measurements came from `src/cli/dsstats.spawnscan.cli`.

- Normal Direct Strike spawn cadence is effectively `1440` gameloops, about `64.29s`.
- In `Direct Strike TE (1928).SC2Replay`:
  - First middle control: `T2@840`.
  - Spawn interval range: `1440..1572`, median `1440`.
  - Spawn window range: `0..3`, median `1`.
  - First spawns: `P1:480-481`, `P4:481-481`, `P5:960-960`, `P6:1441-1441`, `P2:2400-2400`, `P3:1440-1440`.
  - Estimated uncontested movement speed from first mid: `0.1429` world units/gameloop, about `3.20` world units/second.
- A 5-replay local sample produced first-mid speed estimates around `3.04..3.20` world units/second.
- Tutorial replay `Direct Strike (10232).SC2Replay`:
  - Special/tutorial cadence observed: `480` gameloops.
  - Observer spawned at `1440` and disappeared/died at `3376`.
  - Observer lifetime delta: `1936` gameloops, about `86.43s`.
  - Later observer spawns in that replay had missing death data, likely because the replay ended before their max lifetime elapsed or disappearance was not tracked.
- In regular combat replays, many units show lifetime caps around `2095..2098` gameloops, about `93.6s`, but combat deaths make that data noisy.

## Current Playback Assumptions

- Fixed UI stepping remains `5s` (`112` gameloops).
- Canvas rendering may use an effective render gameloop rather than the raw step gameloop.
- When a raw step lands inside a paired spawn window, rendering snaps forward to the completed snapshot end so both paired players' units are visible together.
- Pairing for completed spawn snapshots is by ordered `GamePos` within each team:
  - First Team 1 player pairs with first Team 2 player.
  - Second pairs with second, etc.
- Unit-born gameloops are preserved; only the render time is snapped.
- Alive-unit tables group by team and unit name, not by individual unit.
- Current kills are time-aware and count `KillGameloops <= renderGameloop`.

## Performance Notes

- Playback rendering should not introduce database interactions or extra API calls.
- Static canvas geometry is cached in JavaScript after replay initialization.
- Snapshot metadata is built once while creating `SpawnPlaybackReplay`.
- Alive table rows should only be rebuilt when the effective render gameloop changes.
- Avoid per-frame LINQ-heavy table generation and avoid allocating new canvas geometry on every draw.
- If playback loading feels slow, measure decode/parse/metadata separately before optimizing the canvas path.

## Scanner CLI

The scanner is intentionally separate from the server solution:

```powershell
dotnet run --project src/cli/dsstats.spawnscan.cli/dsstats.spawnscan.cli.csproj -- "C:\path\to\replay.SC2Replay"
```

Scan a replay directory with a limit:

```powershell
dotnet run --project src/cli/dsstats.spawnscan.cli/dsstats.spawnscan.cli.csproj -- "C:\path\to\Replays\Multiplayer" 10
```

The scanner reports decode time, parse time, spawn intervals, spawn windows, first-spawn timings, first-middle speed estimates, and unit lifetime/disappear summaries.

## Open Questions

- Whether playback should model positions from a constant movement speed after spawn completion instead of interpolating to death position.
- Whether the general Direct Strike max unit lifetime is `2096` gameloops, with the tutorial observer's `1936` gameloops being a special case.
- Whether the tutorial replay's one-player parser shape is expected for tutorial maps or needs special handling.
