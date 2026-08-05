# Direct Strike Parser Architecture

This document defines the target architecture for the Direct Strike parser in
`dsstats.parser`. It is the design contract for bringing the maintained parser
implementation back into this project.

## Current integrated state

The maintained Direct Strike parser is compiled directly into
`dsstats.parser`; there is no `Sc2DirectStrike.Parser` NuGet dependency.
`Sc2DirectStrikeParser.Parse` produces the detailed model, and an internal
mapper creates `dsstats.shared.ReplayDto` directly from that model.

The compatibility facade remains `DsstatsParser`. Its replay, import, in-house,
and spawn-playback paths reuse one parsed `DirectStrikeReplay`.

The in-repository parser has this data flow:

```text
s2protocol.NET Sc2Replay
        |
        v
Detailed DirectStrikeReplay
        |
        +--> diagnostics and exact event data
        +--> observers and in-house metadata
        +--> spawn-playback sidecar
        |
        v
dsstats.shared.ReplayDto
```

The decoded replay must be parsed into `DirectStrikeReplay` once. The detailed
model is then mapped directly to the dsstats DTO contract. Do not introduce
another intermediate replay DTO graph between these models.

## Detailed replay and DTO responsibilities

Keep `DirectStrikeReplay` as the detailed representation of replay evidence. It
must retain the information needed for:

- exact spawn waves and unit positions;
- tracker and game-event diagnostics;
- build-unit modification events and analysis status;
- observers and in-house replay metadata;
- spawn-playback sidecar generation.

Map that detailed representation directly to `dsstats.shared.ReplayDto` for
storage, upload, compatibility hashes, and application use. Existing
`DsstatsParser` entry points remain the public facade.

When both a DTO and a sidecar are required, they must reuse the same parsed
`DirectStrikeReplay`. Parsing the replay twice is not acceptable.

## Unit tracking

### The build area defines the roster

Track a unit as part of a player's army only when evidence shows that the unit
was built or morphed in that player's build area.

Maintain a per-player build history from unit-born and unit-type-change events.
Canonical identities from that history form the allowlist for later spawned
units. A unit appearing in a staging or combat area is not sufficient evidence
by itself.

This rule intentionally excludes entities such as:

- summons and temporary units;
- projectiles and effect helpers;
- locusts, broodlings, and other unit-produced entities;
- inventory items and modification helpers;
- map or objective units.

Add a special case only when a fixture proves that a legitimate purchased unit
cannot satisfy the general build-area rule.

### Ownership is explicit

Keep build histories, staging areas, spawn candidates, and modification targets
isolated per player. Resolve ownership from control-player identity, player
layout, unit tags, and build-area evidence.

Do not infer ownership by alternating players or by assuming that only one
player spawns at a time. Kitchen Sink Brawl can spawn every player on a team
simultaneously.

### Event ordering

Unit-born and unit-type-change events are one logical stream. If both inputs
are already ordered by gameloop, merge them without creating and sorting a
combined list. Retain a sorting fallback for unordered or non-indexable input.

For events at the same gameloop, preserve a deterministic order so build and
morph evidence is available before dependent spawn candidates are evaluated.

## Unit-name contract

Raw name, canonical identity, and display representation are different
concepts:

| Layer | Purpose | Example |
| --- | --- | --- |
| Raw tracker name | Preserve replay evidence in reported units | `SwarmHostMP` |
| Parser canonical identity | Match built, morphed, spawned, and modified units | `SwarmHost` |
| Application display representation | UI name and visual/game metadata | `Swarm Host` |

Reported spawn units keep their raw tracker name in `UnitDto.Name`. The parser
performs only the minimum canonicalization needed for identity matching:

- remove an evidenced commander prefix or suffix;
- remove the `Lightweight` and `Starlight` suffixes;
- apply a small immutable table of evidenced exceptional aliases.

Examples:

| Raw tracker name | Canonical identity | `UnitMapNg` display name |
| --- | --- | --- |
| `SwarmHostMP` | `SwarmHost` | `Swarm Host` |
| `PhoenixArtanis` | `Phoenix` | `Phoenix` |
| `VileRoach` | `VileRoach` | `Vile Roach` |

Do not add spaces, localized labels, colors, radii, costs, life values, or other
presentation metadata in the parser. `UnitMapNg` owns canonical application
names and unit representation.

Build-unit modification targets use parser canonical identities. When mapping
a modification count onto `dsstats.shared.UnitDto.Special`, normalize both the
modification target and the raw spawn-unit name through `UnitMapNg` for the
player's commander. Never join a display name directly to a raw tracker name.

## Efficiency requirements

Parsing is a hot path and must not access a database.

Prefer:

- a single parse of each decoded replay;
- one pass over an event family where practical;
- player-indexed arrays for fixed per-player state;
- dictionaries and sets with `StringComparer.Ordinal`;
- span-based name comparisons and lookups;
- frozen tables for immutable aliases;
- per-player caches for canonical strings;
- pre-sized lists based on known event or unit counts.

Avoid:

- per-event string slicing or normalization allocations;
- repeated broad scans of all tracker or game events;
- repeated LINQ pipelines in event-processing loops;
- building temporary collections when ordered streams can be merged;
- database lookups or application-service calls;
- reparsing solely to create another output representation.

Optimize from evidence. Keep exceptional alias and ability tables small,
immutable, and covered by replay fixtures.

## Behavioral guardrails

The following behavior is part of the parser contract:

- player matching remains reliable across player id, toon, slot, and supported
  fallback layouts;
- simultaneous spawning cannot attribute units or modifications to another
  player;
- spawn grouping and breakpoint snapshots remain deterministic;
- unit-produced entities remain excluded;
- modification analysis reports whether it was analyzed, unsupported for the
  data build, or missing required events;
- modification commands remain one command to one eligible unit unless the
  replay explicitly encodes a batch through selected-unit command-manager
  continuation states, as Guardian Shell does, or the mechanic represents a
  stack or consumed source;
- minimum supported data builds are inclusive and fixture-backed;
- replay and player compatibility hashes remain stable unless an intentional
  compatibility-version change is made;
- objective death and player-duration fallbacks continue to produce the
  established replay duration;
- raw reported unit names remain unchanged by application display mapping.

Replay-specific fixes must include the replay as a portable test fixture and
assert the final public DTO, not only an internal helper.

## Testing

Keep fixture coverage for at least:

- standard, commander, and Brawl modes;
- simultaneous Kitchen Sink team spawning;
- built units, morphed units, and lightweight/starlight variants;
- exclusion of summons and unit-produced entities;
- Abathur biomass and the `SwarmHostMP` alias;
- all Artanis Guardian Shell targets, including `PhoenixArtanis`, batched
  selections, and distinct target tags;
- modification breakpoints, command gameloops, target tags, and player
  isolation;
- legacy and forward-compatible data-build boundaries;
- observers, duration, middle control, winner, and layout fallbacks;
- compatibility hashes and spawn-playback sidecars.

Run replay-decoding tests serially while the decoder's shared data-build
initialization remains concurrency-sensitive. Parallel replay decoding can
produce duplicate-key initialization failures that are unrelated to parser
behavior.

## In-repository maintenance

1. Preserve the existing `DsstatsParser` entry points and
   `dsstats.shared.ReplayDto` contract.
2. Map `DirectStrikeReplay` directly to the dsstats DTO and reuse it for
   spawn-playback sidecars.
3. Keep fixture-backed changes in the dedicated serial
   `dsstats.parser.tests` project.
4. Compare raw unit names, player assignment, breakpoints, modification counts,
   and compatibility hashes when importing future changes from the standalone
   parser history.
5. Do not reintroduce a second replay DTO graph or an external parser package.

No database migration or replay backfill is implied by moving the parser. Any
historical reprocessing remains a separate, explicit operation.
