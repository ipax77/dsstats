# Scan Detection

This checklist tracks the cross-repository work required to detect Direct Strike scans and resumed replays efficiently, expose the derived information, and preserve it in dsstats.

## 1. Strongly decode `SCmdEvent` in `s2protocol.NET`

- [x] Implement the typed `SCmdEvent` reader so known command events no longer become `UnknownGameEvent` instances.
- [x] Decode the user ID, gameloop, ability link, ability command index/data, target, command flags, sequence, unit group, and other-unit reference.
- [x] Keep scan classification out of `s2protocol.NET`: a scan is a Direct Strike interpretation of a generic SC2 command event.
- [x] Add a regression test using the existing decoder replay fixture. The first expected command in `test6.SC2Replay` (build 88500) is at gameloop 63 for user 1, with ability link 1113, command index 0, flags 448, and no target.
- [x] Strongly decode the recovery-related events required to determine whether play was resumed from a replay, including `SHijackReplayGameEvent` and `SGameUserJoinEvent` with its `m_hijack` and `m_hijackCloneGameUserId` fields.
- [ ] Evaluate `SSaveGameEvent`, `SSaveGameDoneEvent`, and `SLoadGameDoneEvent` as supporting recovery evidence and strongly decode the fields needed by the final detection rule.

## 2. Detect scans and resumed replays in `Sc2DirectStrike.Parser`

- [x] Decode the complete game-event stream through the Direct Strike decoder options; keep event selection and interpretation in the parser.
- [x] Identify scans by supported data-build ability mappings and command index 0.
- [x] Treat data build `97425` as the minimum supported scan build; keep the mapping enabled
  for newer builds until a specific incompatible build is identified.
- [x] Map each command's `UserId` to the correct Direct Strike player.
- [x] Expose nullable `ScanCount` on the Direct Strike player contract:
  - `null`: game events were not analyzed or the data build is unsupported.
  - `0`: the replay was analyzed using a supported mapping and the player did not scan.
  - Positive value: detected scan count.
- [x] Add a regression test using the supplied replay and assert the exact per-player scan counts.
- [x] Expose replay-level nullable `ResumedFromReplay` on the Direct Strike replay and DTO contracts:
  - `true`: an explicit supported replay-hijack/recovery marker was detected.
  - `false`: the relevant game-event stream was analyzed using a supported protocol and no recovery marker was detected.
  - `null`: game events were not analyzed, the protocol is unsupported, or the available evidence is insufficient to decide safely.
- [x] Base a `true` result primarily on `SHijackReplayGameEvent`; also recognize `SGameUserJoinEvent.m_hijack`.
- [x] Do not infer `false` when the game-event stream was not decoded.
- [ ] Add recovered, normal, and unavailable/unsupported replay tests covering all three `ResumedFromReplay` states.

### 2.1. Raynor free scans

- [x] Verify the Raynor free-scan signature at data build `97563`; the overall scan
  analysis minimum remains `97425`.
- [x] Detect a successful Raynor scan from `SCmdEvent` ability `1142:0`.
- [x] Require both:
  - The issuing player is Raynor.
  - The command has a target point (`TargetX` and `TargetY` are present).
- [x] Do not count the untargeted `1142:0` command emitted during setup. In the supplied
  replay it occurs at gameloop `109`, has command flags `320`, and has no target.
- [x] Keep normal paid scans as the separate ability `1416:0`. Their successful commands
  also carry a target point; do not merge the ability signatures before cost is derived.
- [ ] Decide how the public contract distinguishes paid and free scans. A single
  `ScanCount` is sufficient for total usage, but is insufficient for calculating mineral
  cost safely as `ScanCount * 25`.
- [x] Keep detection in the existing single game-event pass. Use constant comparisons
  and the already-resolved player; do not add another event pass, collection, or database
  interaction.
- [x] Add the supplied Raynor replay as a regression fixture:
  - Path: `C:\data\ds\testreplays\Raynor_Scans\Direct Strike (10897).SC2Replay`.
  - Data build is `97563`; game mode is Brawl Commanders.
  - PAX is game-event user `3`, parser `GamePos` `4`, and Raynor.
  - PAX reaches Tier 2 at gameloop `6360`.
  - There are 46 PAX `1142:0` commands in total: one untargeted setup command and
    45 targeted scan casts.
  - Assert PAX's exact 45 scan gameloops:
    `6419`, `7341`, `7965`, `9039`, `9926`, `10293`, `11504`, `12271`,
    `12626`, `12933`, `13572`, `14584`, `15230`, `15832`, `16134`, `16850`,
    `17928`, `18191`, `19537`, `20276`, `21020`, `22308`, `23556`, `24227`,
    `24793`, `26478`, `27185`, `30316`, `31659`, `32334`, `32731`, `33294`,
    `34038`, `34231`, `34944`, `36215`, `36636`, `37137`, `37997`, `39271`,
    `40097`, `40477`, `42219`, `44060`, and `44551`.

## 3. Add building-area unit modifications to `Sc2DirectStrike.Parser`

- [x] Add a generalized player-level building-area modification timeline:

  ```csharp
  public sealed record DirectStrikeBuildUnitModification(
      BuildUnitModificationType Type,
      int Gameloop,
      int TargetUnitTag,
      string TargetUnitName,
      int Amount,
      int? SourceUnitTag,
      string? SourceUnitName);
  ```

- [x] Add `BuildUnitModificationType` with initial values `Biomass`, `PowerOverwhelming`, `GuardianShell`, `OrbitalStrikeBeacon`, and `DarkPylon`.
- [x] Treat unit tags as replay-local identifiers. Preserve them so repeated modifications can be grouped by an individual placed unit, but do not use them as cross-replay identifiers.
- [x] Add inexpensive per-type analysis status so consumers can distinguish:
  - Analyzed with no modifications.
  - Not analyzed because required event streams were unavailable.
  - Unsupported data build or modification type.
- [x] Expose the detailed modification timeline and analysis status through `DirectStrikePlayer`.
- [x] Expose compact counts through `ReplayPlayerDto.BuildUnitModificationAnalysis` and `SpawnDto.BuildUnitModifications`:
  - Count distinct `(modification type, target unit tag)` values grouped by normalized target name.
  - Use inclusive fixed cutoffs at gameloops `6720`, `13440`, and `20160`; `All` includes every modification.
  - Repeated Biomass and Power Overwhelming applications remain detailed events but count their target only once.
  - Do not add detailed tags, sources, or amounts to `ReplayDto`.
- [x] Reuse the parser's existing build-area and unit-tag indexes. Do not repeatedly scan tracker collections or create a second complete unit graph.
- [x] Keep modification data out of the compatibility hash.

### 3.1. Abathur biomass

- [x] Treat data build `97563` as the minimum build for Abathur biomass.
- [x] Identify the issuing Abathur player from `SCmdEvent.UserId`.
- [x] Detect a manual biomass application by correlating:
  - `SCmdEvent` ability link `1124`.
  - A following `SUnitBornEvent` within one or two gameloops.
  - `UnitTypeName == "BiomassItem"`.
  - `CreatorAbilityName == "InventoryUnit"`.
  - The biomass item and command belonging to the same player.
- [x] Resolve the biomass item's creator unit tag to the modified building-area unit.
- [x] Derive the biomass amount as `SCmdEvent.AbilCmdIndex + 1`.
- [x] Do not treat uncorrelated `BiomassItem` births as manual modifications. Biomass is also copied automatically to spawned wave units.
- [x] Normalize internal target names for display while retaining a stable raw name where required:
  - `AbathurMutalisk` → Mutalisk
  - `ViperAbathur` → Viper
  - `GuardianStarlight` → Guardian
  - `VileRoach` → Roach
  - `SwarmHostAbathur` → Swarm Host
- [x] Add the supplied Abathur replay as a regression fixture and assert PAX's exact ten modifications and 36 total biomass:

  | Time | Target | Amount |
  |---|---|---:|
  | 2:06.651 | Mutalisk | 3 |
  | 4:54.821 | Mutalisk | 3 |
  | 7:52.544 | Mutalisk | 3 |
  | 11:04.776 | Mutalisk | 3 |
  | 14:38.883 | Mutalisk | 3 |
  | 18:52.410 | Viper | 5 |
  | 22:55.625 | Guardian | 4 |
  | 26:52.232 | Guardian | 4 |
  | 30:22.410 | Roach | 3 |
  | 35:10.848 | Swarm Host | 5 |

### 3.2. Alarak Power Overwhelming

- [x] Treat data build `97563` as the minimum build for Alarak Power Overwhelming.
- [x] Detect a building-area sacrifice from `SUnitDiedEvent` and its linked tracker units:
  - Victim type is `SupplicantStarlight`.
  - Victim creator ability is `SupplicantPlace`.
  - Killer type is `AscendantStarlight`.
  - Killer creator ability is `AscendantPlace`.
  - Victim, killer, and parsed player belong to the same Alarak player.
- [x] Record the killer Ascendant as the target unit, the Supplicant as the source unit, and amount `1`.
- [x] Do not require a game command. Ability `1074:0` corroborates manual activations, but autocast sacrifices have no corresponding `SCmdEvent`.
- [x] Exclude battlefield-copy sacrifices. Their born events do not use the `SupplicantPlace` and `AscendantPlace` creator abilities.
- [x] Add the supplied Alarak replay as a regression fixture and assert:
  - PAX is user 5 / player 6.
  - 36 PAX Supplicant-by-Ascendant deaths exist in total.
  - 22 are building-area Power Overwhelming modifications.
  - 14 battlefield-copy sacrifices are excluded.
  - Ascendant tag `76808195` receives 13 modifications.
  - Ascendant tag `76283920` receives 9 modifications.
- [x] Assert the eight early building-area modifications:
  - 7:05.758
  - 7:08.392
  - 8:13.705
  - 8:23.973
  - 9:15.982
  - 9:19.821
  - 10:21.250
  - 10:24.598
- [x] Assert the fourteen tracked building-area modifications:
  - 11:08.303
  - 11:22.321
  - 12:10.044
  - 12:21.919
  - 13:08.392
  - 13:22.901
  - 14:13.928
  - 14:45.133
  - 15:19.821
  - 15:32.678
  - 16:52.098
  - 17:05.758
  - 18:06.517
  - 19:05.491

### 3.3. Artanis Guardian Shell

- [x] Treat data build `97425` as the minimum build for Artanis Guardian Shell.
- [x] Identify the issuing Artanis player from `SCmdEvent.UserId`.
- [x] Detect command index `0` with the unit-specific Guardian Shell ability link:
  - `1114` → `HonorGuard`
  - `1117` → `HighArchon`
  - `1119` → `ArtanisObserver`
  - `1120` → `ImmortalArtanis`
- [x] Resolve the target from that player's not-yet-modified persistent building-area units of the mapped type:
  - Require the unit's `SUnitBornEvent` to use the corresponding `*Place` creator ability.
  - Reuse the existing build-area unit-tag index.
  - Do not use a short birth-to-command time window. The sample contains an Immortal that receives Guardian Shell 6,213 gameloops after placement.
  - Record the modification only when exactly one eligible unit can be resolved; do not guess a target tag when multiple candidates remain.
- [x] Record the resolved building-area unit as the target, no source unit, and amount `1`.
- [x] Do not require or search for a tracker-side buff event. The sample has no Guardian Shell birth or type-change event.
- [x] Exclude Shield Overcharge. It is the separate global ability `1242:0` and does not modify the Guardian Shell timeline.
- [x] Keep the detector allocation-light: use the existing unit/tag indexes plus small per-player/per-unit-type candidate state, scan the already-decoded game-event stream once, and introduce no database interaction in the parser.
- [x] Add the supplied Artanis replay as a regression fixture:
  - Data build is `97425`.
  - PAX is game-event user `2`, zero-based lobby slot `2`, and parser `GamePos` `3`.
  - The command signature produces 16 Guardian Shell candidates: 10 Honor Guards, 1 Observer, 2 Immortals, and 3 High Archons.
  - Confirm visually whether the additional High Archon command at gameloop `19911` (`14:48.884`) is Guardian Shell. It was absent from the original hand-written list, but has the same `1117:0` signature and uniquely resolves to the High Archon placed at gameloop `19864`.
  - Assert the exact command gameloops and resolved replay-local target tags after that visual check:

    | Gameloop | Replay time | Target | Target unit tag |
    |---:|---:|---|---:|
    | 354 | 0:15.804 | Honor Guard | 74973186 |
    | 2287 | 1:42.098 | Honor Guard | 79691781 |
    | 2493 | 1:51.295 | Honor Guard | 54263819 |
    | 3879 | 2:53.170 | Honor Guard | 29097989 |
    | 3981 | 2:57.723 | Honor Guard | 15204357 |
    | 5573 | 4:08.795 | Honor Guard | 52690962 |
    | 5641 | 4:11.830 | Honor Guard | 39059473 |
    | 7178 | 5:20.446 | Honor Guard | 58458129 |
    | 8239 | 6:07.813 | Honor Guard | 25165848 |
    | 8728 | 6:29.643 | Honor Guard | 34603042 |
    | 10071 | 7:29.598 | Observer | 51118121 |
    | 14328 | 10:39.643 | Immortal | 16252945 |
    | 18604 | 13:50.536 | High Archon | 59506809 |
    | 18714 | 13:55.446 | Immortal | 305659917 |
    | 19911 | 14:48.884 | High Archon | 301465660 |
    | 21256 | 15:48.929 | High Archon | 288358467 |

- [x] Assert the three Shield Overcharge commands are excluded: gameloops `6147`, `11944`, and `17836`.

### 3.4. Karax Orbital Strike Beacon

- [x] Treat data build `97425` as the minimum build for Karax Orbital Strike Beacon.
- [x] Identify the issuing Karax player from `SCmdEvent.UserId`.
- [x] Detect Orbital Strike Beacon from `SCmdEvent` ability `2014:0`.
- [x] Expose and use `m_data.TargetUnit.m_tag` from the command payload:
  - The current typed `SCmdEvent` preserves the target snapshot point but not the target unit tag.
  - Do not infer the target from snapshot coordinates; two placed units can be equally close.
  - Resolve the target tag through the existing build-area unit-tag index in O(1).
- [x] Require the resolved target to be a persistent placed unit owned by the same Karax player. Do not hard-code Mirage as the only valid target type; all targets happen to be Mirages in this fixture.
- [x] Record the resolved building-area unit as the target, no source unit, and amount `1`.
- [x] Treat the command as authoritative. The sample contains no corresponding Orbital Strike Beacon tracker birth or unit-type change.
- [x] Reject commands whose target tag is absent from the player's build-area index or has already received Orbital Strike Beacon.
- [x] Keep the detector allocation-light: extend the existing static data-build ability mapping, scan the already-decoded game-event stream once, reuse the target-tag dictionary, and introduce no database interaction in the parser.
- [x] Add the supplied Karax replay as a regression fixture:
  - Data build is `97425`.
  - PAX is game-event user `0` and parser `GamePos` `1`.
  - The replay has three Karax players and 14 `2014:0` commands in total: PAX has 5, RayMith has 5, and Fando has 4.
  - All 14 target persistent `MiragePlace` units owned by the issuing player.
  - Assert PAX's exact five modifications:

    | Gameloop | Replay time | Target | Target unit tag |
    |---:|---:|---|---:|
    | 4588 | 3:24.821 | Mirage | 62390277 |
    | 9021 | 6:42.723 | Mirage | 77856771 |
    | 13494 | 10:02.411 | Mirage | 27000851 |
    | 17970 | 13:22.232 | Mirage | 77594634 |
    | 23111 | 17:11.741 | Mirage | 62652429 |

### 3.5. Vorazun Dark Pylon

- [x] Treat data build `97425` as the minimum build for Vorazun Dark Pylon.
- [x] Detect a Dark Pylon application from `SUnitBornEvent` and its creator unit:
  - `UnitTypeName == "VorazunDarkPylon"`.
  - `CreatorAbilityName == "InventoryUnit"`.
  - The creator tag resolves to a persistent building-area unit whose creator ability is the corresponding `*Place` ability.
  - The Dark Pylon birth, creator unit, and parsed player belong to the same Vorazun player.
- [x] Record the creator unit as the target, no source unit, and amount `1`.
- [x] Do not require a game command. Ability `2013:0` corroborates direct applications, but when Tier 3 is reached the sample automatically backfills Dark Pylon onto six additional existing Void Rays without separate commands.
- [x] Exclude battlefield-copy Dark Pylons. Their creator units are spawned wave copies and do not have a `*Place` creator ability.
- [x] Normalize `VoidRayVorazun` to `Void Ray` for display while retaining the raw unit name where required.
- [x] Keep the detector allocation-light: classify each `VorazunDarkPylon` birth during the existing tracker pass, resolve its creator through the existing unit-tag dictionary, and introduce no second tracker scan or database interaction.
- [x] Add the supplied Vorazun replay as a regression fixture:
  - Data build is `97425`.
  - PAX is game-event user `0` and parser `GamePos` `1`.
  - PAX has 54 `VorazunDarkPylon` births in total.
  - Of these, 13 target persistent building-area Void Rays and must be recorded.
  - The other 41 belong to battlefield copies and must be excluded.
  - Seven `2013:0` commands corroborate the first and the six later direct applications; six Tier-3 backfill applications have no command.
  - Assert PAX's exact 13 modifications:

    | Gameloop | Replay time | Target | Target unit tag |
    |---:|---:|---|---:|
    | 15203 | 11:18.705 | Void Ray | 43778052 |
    | 15216 | 11:19.286 | Void Ray | 91750427 |
    | 15228 | 11:19.821 | Void Ray | 85721102 |
    | 15244 | 11:20.536 | Void Ray | 30933009 |
    | 15257 | 11:21.116 | Void Ray | 267124745 |
    | 15290 | 11:22.589 | Void Ray | 38010921 |
    | 15305 | 11:23.259 | Void Ray | 273416208 |
    | 16104 | 11:58.929 | Void Ray | 305397774 |
    | 17439 | 12:58.527 | Void Ray | 277086251 |
    | 18879 | 14:02.813 | Void Ray | 328990730 |
    | 19044 | 14:10.179 | Void Ray | 308019232 |
    | 20235 | 15:03.348 | Void Ray | 60031075 |
    | 20566 | 15:18.125 | Void Ray | 297271338 |

- [x] Assert the seven corroborating `2013:0` command gameloops: `15202`, `16103`, `17438`, `18878`, `19043`, `20234`, and `20565`.
- [x] Verify the supplied data-build `97563` replay `Direct Strike (10899).SC2Replay`:
  - PAX is parser `GamePos` `4`.
  - PAX has five placed `VoidRayVorazun` units.
  - Three have Dark Pylon by the 15-minute breakpoint and all five have it at `All`.
  - Use the shared minimum-build thresholds; keep the existing single tracker pass, indexes,
    allocation behavior, and zero database interaction unchanged.

## 4. Preserve scan and building-area information in dsstats

- [ ] Carry nullable `ScanCount` through replay DTOs and mappings.
- [ ] Carry nullable `ResumedFromReplay` through replay-level DTOs and mappings.
- [ ] Carry compact breakpoint modification counts and per-type analysis status through dsstats DTOs and mappings.
- [ ] Add nullable `ScanCount` to the replay-player database entity.
- [ ] Add nullable `ResumedFromReplay` to the replay database entity.
- [ ] Choose a provider-compatible persistence representation for breakpoint counts; keep detailed replay-local unit tags and amounts in `DirectStrikePlayer` only.
- [ ] Add migrations for MySQL, PostgreSQL, and SQLite. Existing scan and resumed-replay values must remain `NULL`.
- [ ] Preserve `ScanCount` and `ResumedFromReplay` through API contracts and all replay ingestion paths.
- [ ] Preserve compact breakpoint modification counts and analysis status through API contracts and all replay ingestion paths.
- [ ] Configure every dsstats replay-decoding entry point to enable the parser's complete game-event decoding options.
- [ ] Do not add scan count to the existing compatibility hash; preserve hash compatibility for existing replays.
- [ ] Display the scan count in the replay team/player table.
- [ ] Display or provide a tooltip for the mineral cost at 25 minerals per paid scan.
  Raynor ability `1142:0` scans are free and must not contribute to mineral cost.
- [ ] Show a dash for unknown (`null`) and `0` for a supported, analyzed replay without scans.
- [ ] Surface `ResumedFromReplay` in replay details as Yes, No, or Unknown; do not treat Unknown as No.
- [ ] Make `ResumedFromReplay` available to duplicate detection as an explicit signal, without automatically discarding a replay solely because the value is `true`.
- [ ] Add mapping, persistence, API serialization, and UI tests for `null`, `0`, and positive scan counts and all three resumed-replay states.
- [ ] Add mapping, persistence, API serialization, and UI tests for supported, empty, unavailable, and unsupported breakpoint modification summaries.

## 5. Investigate the decoder's allocation volume

- [ ] Reproduce the end-to-end benchmark in Release mode with a stable .NET 10 SDK and the same four-replay corpus.
- [ ] Verify BenchmarkDotNet's per-operation allocation accounting, including invocation count and unroll behavior. The reported value is cumulative managed allocation, not necessarily simultaneously retained memory.
- [ ] Record the replay file sizes, event counts, decoded object counts, total managed allocation, peak working set, and retained managed heap for each replay individually.
- [ ] Split the benchmark into independently measurable phases:
  - MPQ archive opening and member extraction/decompression.
  - Protocol/type-info loading with cold and warm caches.
  - Header, init data, details, and metadata.
  - Tracker-event decoding.
  - Game-event decoding.
  - Tracker unit-link construction.
  - Direct Strike parsing and DTO creation.
- [ ] Capture allocation stack traces with an appropriate .NET profiler and identify the dominant allocated types and call sites.
- [ ] Specifically inspect:
  - Temporary MPQ and decompression buffers.
  - Bit-packed and versioned decoder intermediate objects.
  - Repeated strings and UTF-8 decoding.
  - Tracker and game-event list growth/copies.
  - Unit-tag dictionaries and tracker connection graphs.
  - LINQ enumerators, temporary arrays, and DTO collection copies.
  - Work repeated for every replay that could be cached safely.
- [ ] Compare allocation with game events disabled and enabled so the approximately 5.7 MiB incremental game-event cost is separated from the much larger pre-existing baseline.
- [ ] Determine why the four-replay pipeline reports approximately 7.8 GB of managed allocation, roughly 1.95 GB per replay, despite replay files being only a few megabytes.
- [ ] Distinguish high allocation throughput from an actual retention/leak problem by forcing a collection after each phase and measuring the surviving heap.
- [ ] Add focused benchmarks or regression guards around any corrected dominant allocation source.
- [x] Re-run CPU and allocation benchmarks after building-area modification detection is implemented and confirm it does not introduce another complete tracker scan or material unit-graph duplication.
  - Focused five-replay ShortRun: approximately `8.94 ms` and `5.37 MB` allocated for parsing plus breakpoint DTO creation.
  - A large-struct dictionary experiment allocated `5.75 MB`; retaining compact dictionary references measured better and was kept.

## Research notes

- Sample replay: `C:\data\ds\testreplays\scan\Direct Strike (1155).SC2Replay`.
- The scan is directly represented in game events as `NNet.Game.SCmdEvent`.
- Confirmed sample signature:
  - Ability link: `1416`
  - Ability command index: `0`
  - `m_userid` identifies the issuing player.
  - `m_gameloop` identifies the command time.
- Confirmed anchor commands:
  - CoughTots: gameloop 1829 (about 81.65 seconds).
  - HotDog: gameloop 2842 (about 126.88 seconds).
  - BadMoon: gameloop 4059 (about 181.21 seconds).
- Confirmed counts in the supplied replay:

  | Player | Scans | Mineral cost |
  |---|---:|---:|
  | HotDog | 4 | 100 |
  | NoNsenSe | 4 | 100 |
  | CoughTots | 1 | 25 |
  | PAX | 10 | 250 |
  | Mahala | 3 | 75 |
  | BadMoon | 5 | 125 |
  | **Total** | **27** | **675** |

- Team 2 (PAX, Mahala, and BadMoon) has 18 detected scans in this replay.
- PAX scan gameloops: 5671, 9919, 11399, 14277, 16723, 18288, 20859, 22936, 24060, and 25187.
- Tracker events contain no explicit scan event. Periodic mineral snapshots can indirectly validate the 25-mineral deduction but are not reliable enough for primary detection.
- The game-event protocol contains `SHijackReplayGameEvent`. `SGameUserJoinEvent` additionally contains `m_hijack` and `m_hijackCloneGameUserId`, which can support resumed-replay detection.
- Ability-link IDs can change with SC2 data builds. Ability link 1416 is confirmed for data
  builds 97425 and 97563 and is treated as forward-compatible from build 97425. If a later
  incompatible build is found, represent it explicitly. Earlier observed candidates (1409
  and 1415) remain unsupported.
- Raynor uses a distinct free-scan command in data build `97563`: ability `1142`,
  command index `0`. Successful casts have a point target and command flags `256`.
- In the Raynor sample, PAX issues 46 `1142:0` commands. The command at gameloop
  `109` is not a cast: it has flags `320`, no target, and predates Tier 2. The remaining
  45 commands all have point targets and begin at gameloop `6419`, immediately after
  Tier 2 at `6360`.
- Tracker events contain `RaynorScanModification` at gameloop `1`, but no per-cast
  scan event. It describes the commander setup and cannot be used to count scans.
- The brawl replay emits multiple `StagingAreaNextSpawn` upgrades per shared spawn.
  Those events can corroborate charge grants but are unnecessary and ambiguous for
  counting actual casts. The targeted `SCmdEvent` is authoritative.
- Full game-event materialization for the sample produced about 9,506 event objects and added roughly 5.3 MB of allocation.
- Optional event filters were prototyped and removed. They could not avoid traversing the variable-length bitstream, saved only about 1.6 MB per replay for full game events, and did not demonstrate a CPU benefit worth the added public API.
- The end-to-end MediumRun benchmark decodes and creates DTOs for four replays per operation. Tracker-only averaged 1.248 seconds; tracker plus complete game events averaged 1.335 seconds, an increase of about 7.0%.
- Complete game events added about 23.9 MB per four-replay operation, or about 5.7 MiB of managed allocation per replay.

### Building-area modification research

- Abathur sample: `C:\data\ds\testreplays\abathur_biomas\Direct Strike (1164).SC2Replay`.
- The Abathur sample is data build `97563` and contains two Abathur players.
- It contains 173 PAX-controlled `BiomassItem` births, most caused by automatic propagation to spawned waves. Tracker events alone cannot distinguish manual applications safely.
- Across both Abathur players, all 19 ability-`1124` commands matched exactly one same-player `BiomassItem` birth one gameloop later; there were no unmatched commands.
- The biomass item's creator unit tag directly resolves to the modified building-area unit.
- The observed biomass amount is `AbilCmdIndex + 1`: command indexes 2, 3, and 4 represent amounts 3, 4, and 5.
- Alarak sample: `C:\data\ds\testreplays\alarak_poweroverwelming\Direct Strike (1165).SC2Replay`.
- The Alarak sample is data build `97563`; PAX is Alarak in player position 6 and game-event user 5.
- Ability `1074:0` is Power Overwhelming, but it appears for only 12 of PAX's 22 building-area sacrifices because other activations are automatic/autocast.
- Tracker unit relationships are authoritative for Power Overwhelming. Requiring both `*Place` creator abilities cleanly separates persistent building-area units from spawned battlefield copies.
- Artanis Guardian Shell sample: `C:\data\ds\testreplays\artanis_guardianshell\Direct Strike (10853).SC2Replay`.
- The Artanis sample is data build `97425`; PAX is game-event user 2, zero-based lobby slot 2, and parser `GamePos` 3.
- Guardian Shell is represented by unit-specific `SCmdEvent` ability links: `1114:0` for Honor Guard, `1117:0` for High Archon, `1119:0` for Observer, and `1120:0` for Immortal.
- These commands have no target point, `OtherUnit`, or `UnitGroup`, and the tracker stream contains no Guardian Shell unit birth or type change. The target must be resolved against eligible persistent build-area units of the ability's mapped type.
- Every Guardian Shell command in the sample has exactly one eligible, not-yet-modified building-area target. Placement-to-command delay ranges from 28 to 6,213 gameloops, so proximity alone is not a safe rule.
- The replay contains 16 Guardian Shell command candidates. The original notes listed 15; the additional candidate is High Archon ability `1117:0` at gameloop `19911` (`14:48.884`), resolving uniquely to unit tag `301465660`.
- Shield Overcharge is ability `1242:0`; PAX uses it at gameloops `6147`, `11944`, and `17836`. It is distinct from Guardian Shell and should be excluded.
- Karax Orbital Strike Beacon sample: `C:\data\ds\testreplays\karax_OrbitalStrikeBeacon\Direct Strike (10822).SC2Replay`.
- The Karax sample is data build `97425`; PAX is game-event user 0 and parser `GamePos` 1.
- Orbital Strike Beacon is command ability `2014:0`. Its raw `TargetUnit.m_tag` is the authoritative target identifier.
- The typed `SCmdEvent` currently drops `TargetUnit.m_tag` and retains only the snapshot coordinates. Coordinate matching is unsafe: PAX's second command snapshot is equally close to two placed Mirages, while the raw tag resolves unambiguously to `77856771`.
- There is no corresponding Orbital Strike Beacon tracker birth or type change in the sample.
- The sample's three Karax players issue 14 Orbital Strike Beacon commands: PAX has 5, RayMith has 5, and Fando has 4. All 14 raw target tags resolve to same-player persistent `MiragePlace` units.
- Vorazun Dark Pylon sample: `C:\data\ds\testreplays\Vorazun_DarkPylon\Direct Strike (10844).SC2Replay`.
- The Vorazun sample is data build `97425`; PAX is game-event user 0 and parser `GamePos` 1.
- A Dark Pylon application creates `VorazunDarkPylon` through `InventoryUnit`; its creator tag directly identifies the modified unit.
- PAX has 54 such births: 13 creators are persistent `VoidRayVorazunPlace` units and 41 creators are battlefield copies without a `*Place` creator ability.
- Ability `2013:0` appears seven times and is followed one gameloop later by a matching Dark Pylon birth. It does not cover six additional existing Void Rays that are backfilled automatically after Tier 3, so tracker relationships are authoritative.

## Verification and packaging

- [x] Benchmark tracker-only and complete game-event decode-plus-DTO pipelines across four representative replays.
- [x] Measure elapsed CPU time and allocated bytes for the end-to-end parser workflow.
- [x] Verify that the measured CPU overhead stays below the agreed 30% integration threshold. The MediumRun result was about 7.0%.
- [x] Record the additional managed allocation before enabling the feature everywhere. The measured increase was about 5.7 MiB per replay.
- [x] Verify the supplied replay's exact scan counts, user-to-player mapping, and mineral costs.
- [ ] Add or obtain known normal and resumed/recovered replay fixtures and verify `ResumedFromReplay` as `false` and `true` respectively; verify `null` when game events or protocol support are unavailable.
- [x] Pack the updated `s2protocol.NET` package (version 0.9.6) into a temporary local NuGet feed.
- [x] Pack the updated `Sc2DirectStrike.Parser` package (version 0.2.6) into the same temporary feed.
- [x] Test dsstats against the locally packed packages, including mydsstats and replay `10899`.
- [x] Do not publish either package as part of this work.
