# Stats AI validation — 2026-09-15

## Implementation checks

- Web project: builds successfully with no warnings or errors.
- Evaluation tool: builds successfully.
- Repository tests: 590 passed, 4 skipped. Follow-up contract and lifecycle checks passed after the final adjustments.
- Full server solution: blocked by the existing NU1605 downgrade in dsstats.arcade.migration (s2protocol.NET 0.9.7 versus the parser's 1.0.0 requirement).

## Browser checks

Verified the local web app against the public read-only statistics API:

- Synergy and Timeline render data and select the named commander in the chart.
- Manual commander/period changes update the URL; reloading retains those settings.
- Ask stats remains mounted across supported tabs and is hidden on Count.
- An unavailable browser model leaves manual statistics usable.
- A failed statistics load displays an error and permits another request.

The connected in-app browser does not expose the model, and Chrome was not connected to browser automation. Actual browser inference, downloads and model cancellation were not verified. Automated component lifecycle tests cover cancellation of superseded requests, late responses and failure recovery.

## LM Studio evaluation

Model: google/gemma-4-12b, temperature 0, 4096-token response budget, three-minute timeout. These results describe this local model, not Chrome's model.

The initial 28-case run passed 23 cases. Three duration-ranking questions incorrectly selected Series; one winrate request timed out and one filtered synergy request returned no JSON. Successful requests had a median latency of 27.5 seconds. The initial prompt contained 2,571 system-message characters and 4,736 message characters including examples, excluding the user question and response schema.

The prompt was then clarified to distinguish ranked duration buckets from an unranked series. Its ranked Timeline example now exercises a top-N request with explicit winrate. The six-case follow-up passed all six cases: all five initial failures plus the full-series control case. Median latency was 29.0 seconds, with no timeouts or invalid JSON. The revised prompt uses 2,700 system-message characters and 4,914 message characters including examples.

The final prompt SHA-256 is `3DE62F94C41409A0EC234A91286EF927055BBCFFE7FB14228F6C10A52A8BCDA8`. The initial full run used `FA25061C9490F4DE76C0CA73BC991FDBC741A9FE0197B3A7A2AE96C7775AE42F`. The full suite was not rerun after this focused prompt correction.

A legacy winrate v2 smoke evaluation also passed (the best-commander case, 10.4 seconds), confirming the former compatibility path before it was retired. The obsolete winrate bundles and evaluator path have since been removed.

Raw reports are stored locally under the ignored artifacts/stats-ai directory. Each report includes the prompt hash so the initial and revised prompts remain distinguishable.

## Legacy cleanup

Removed the retired winrate prompt bundles, types and evaluator compatibility path. Shared validation and request mapping now live in StatsQueryPlan. All seven current AI tests pass; the evaluation tool and web app build successfully. The web build used a separate output directory because the normal executable was running. The active stats prompt was unchanged.
