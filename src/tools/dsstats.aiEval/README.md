# Browser statistics query evaluation

The website translates English questions into one validated plan for Winrate,
Synergy or Timeline. C# maps the plan to the existing statistics API and selects
the answer; the model does not generate statistics or SQL.

Supported: commander/opponent rankings, teammate/pair rankings, and one named
commander's ranked duration buckets or complete duration series. Metrics are
average rating gain and raw winrate. Timeline measures game duration, not calendar
trends. Each question starts with fresh filters.

## Website configuration

The default bundle is `AI/Prompts/stats/v1.json`. Select a version with
`StatsAI:PromptVersion` (environment variable `StatsAI__PromptVersion`).
This replaces the website's former `WinrateAI:PromptVersion` setting.
Bundles are validated at startup and copied to build/publish output. Restart
after editing or selecting a version.

Keep ordinary Commanders mode, no leavers, Last90Days / Last12Months / AllTime,
inclusive rating bounds 500–3000, balanced teams at 40–60%, and long games at
15 minutes or more. Popularity, exact named-pair/opponent statistics, calendar
trends, arbitrary duration ranges, early/late presets and all-commander timelines
remain unsupported.

## LM Studio evaluation

The opt-in console runner uses the same prompt, parser, validator and request
mapper as the website without calling the statistics API. It links the AI C#
sources rather than maintaining another implementation.

With LM Studio running and a model loaded, from the repository root:

```powershell
dotnet run --project src/tools/dsstats.aiEval -- --output artifacts/stats-ai-evaluation.json
```

Create the output directory first if necessary. Defaults:
`http://localhost:1234/v1/`, `google/gemma-4-12b`, one pass through
`Cases/stats-v1.json`, and the website's `AI/Prompts/stats/v1.json`.

Override with `--endpoint`, `--model`, `--repeat` (1–20), `--prompt`, and `--cases`.
Use `--case timeline-series` to investigate one case. Paths are relative to the
working directory. Cases run sequentially with temperature 0, a 4096-token
response budget and a three-minute timeout per request. Ctrl+C cancels.

Reports include expected/actual plans, semantic differences, raw responses,
timings, prompt version/hash, contract name, system-message character count and
total message character count including examples. Character counts are not token
counts. Failures and cancellation return exit code 1. Unsupported cases compare
the outcome rather than unused plan fields.

### Updating prompts

Copy a bundle to the next version and change `promptVersion`. Keep
`formatVersion=1` while the bundle format stays the same. Compress repeated rules,
but retain explicit defaults and rejection boundaries. Use examples to distinguish
opponents from teammates and ranked duration buckets from a chronological series.
Keep evaluation questions outside the few-shot examples.

## Verification

```powershell
dotnet build src/server/server.sln
dotnet build src/tools/dsstats.aiEval
dotnet test src/tests/dsstats.tests/dsstats.tests.sln
```

The local browser library reference defaults to
`C:\Users\pax77\source\repos\blazor.ai\src\Blazor.AI.BrowserModels\Blazor.AI.BrowserModels.csproj`.
Override with `-p:BrowserModelsProjectPath=...`.

### Browser smoke test

Start the API and web projects and open `/winrate` in compatible desktop Chrome.
Initialize the model and try:

- Best commander, then Kerrigan's worst matchup.
- Best teammate for Kerrigan, then best pairs by winrate.
- When Kerrigan is strongest, then her full winrate series by game duration.
- Another question within the same tab, with a different commander and period.
- Manual filters, cancellation, failed statistics loads, rapid tab changes and
  a reload. A late response must not replace a newer answer.
- Confirm the interpreted settings, selected tab, URL, chart and answer agree.
  Duration-series rows are chronological, include every nonempty returned bucket,
  and label the final interval `35+ min`.
- Confirm Ask stats remains mounted across the three supported tabs and is absent
  on Count. Manual navigation clears the answer; an AI-triggered tab switch keeps it.
- With an unavailable browser model, ordinary statistics controls must still work.

Saved URLs restore statistics settings; questions and answers are transient.
Gemma evaluation measures that model's translation accuracy. It does not establish
Chrome model accuracy or validate browser inference, downloads or cancellation.

See [VALIDATION.md](VALIDATION.md) for the recorded implementation and model checks.
