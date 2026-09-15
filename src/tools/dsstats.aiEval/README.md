# Winrate query evaluation

The website uses the browser model. This opt-in console runner tests the same prompt,
parser, validator and request mapper against LM Studio without calling the statistics API.
The AI C# sources are linked from the web project, so the runner and unit tests do not
need a browser dependency or maintain a second implementation.

From the repository root, with the model loaded and the LM Studio server running:

```powershell
dotnet run --project src/tools/dsstats.aiEval -- --output ai-evaluation-v1.json
```

Defaults: `http://localhost:1234/v1/`, `google/gemma-4-12b`, one pass through
`Cases/v1.json`, and the website's `AI/Prompts/winrate/v1.json` prompt bundle.
Override with `--endpoint`, `--model`, `--repeat` (1–20), `--prompt`, and `--cases`.
Use `--case paraphrase` (or another case ID) to investigate one case quickly.
Use `--help` for syntax. Paths are relative to the working directory.

Cases run sequentially with temperature 0, a 4096-token response budget and a
three-minute timeout per request. Ctrl+C cancels; failures and cancellation return
exit code 1. Reports contain prompts/questions, expected and actual plans, semantic
differences, raw responses, timings, model, prompt version and SHA-256 hash.
Unsupported cases compare the outcome rather than unused plan fields.

## Updating prompts

Copy `v1.json` to `v2.json`, update `promptVersion`, then edit its ordered
`systemMessages`, examples or schema. `formatVersion` describes the file format;
keep it at 1 unless the loader changes. Test candidate versions with `--prompt`.
Keep evaluation questions in the separate cases file, outside the few-shot examples.

Select a website bundle with `WinrateAI:PromptVersion` (environment variable
`WinrateAI__PromptVersion`), default `v1`. Bundles are validated at startup and
copied to build/publish output. Restart after editing or selecting a version.

`v2.json` is an included candidate that adds explicit mappings for matchup idioms
such as "struggles most". Compare it without editing v1:

```powershell
dotnet run --project src/tools/dsstats.aiEval -- --prompt src/server/dsstats.web/AI/Prompts/winrate/v2.json --output ai-evaluation-v2.json
dotnet run --project src/server/dsstats.web -- --WinrateAI:PromptVersion=v2
```

The local library reference defaults to:
`C:\Users\pax77\source\repos\blazor.ai\src\Blazor.AI.BrowserModels\Blazor.AI.BrowserModels.csproj`.
Override it at build time with `-p:BrowserModelsProjectPath=...`.
The local library project selects .NET 10 under this repository's SDK, and both
.NET 10 and .NET 11 under its own .NET 11 SDK.

## Browser smoke test

Start the API and web projects, open `/winrate` in a compatible desktop Chrome,
initialize the model, and ask a sample question. Check that the interpreted settings,
URL, chart and factual answer agree. Try manual filters, another question, cancellation,
tab changes and a reload. Other stats tabs must not show the AI panel.
An unavailable browser model must leave ordinary statistics usable.

Gemma evaluation measures prompt translation with that model. It does not establish
Chrome's model accuracy or validate browser inference, downloads or cancellation.
