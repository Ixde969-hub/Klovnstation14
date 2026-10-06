# SS13 → SS14 port pipeline

Tooling for porting SurfShack13 (BYOND /tg/ fork) content to an SS14 fork.
Lives in the Klovnstation14 fork (`Ixde969-hub/Klovnstation14`, branch `port-pipeline`) under `Tools/_KS14/port-pipeline/`.

## Approach

1. **Inventory** — what SurfShack has that is unique, and whether the SS14 base already has it.
2. **Data converters** — rule-based DM → YAML for pure-data content (no AI).
3. **Agent loop** — per feature: context → generate → build → test → fix, with
   design questions parked for a human (see `patterns.md`).

## Model routing

| Task | Runs on |
|---|---|
| Summarising/classifying DM, drafting YAML/localization, bulk extraction | Hermes (`tools/hermes_task.py`, cheap model via OpenRouter) |
| Behaviour ports, hard build fixes, review, patterns doc | Claude |

Hermes runs **sandboxed**: only the `todo` toolset and `--ignore-rules`, so it can't read/write
files or run commands. Callers inline all source it needs. Every call is logged in `logs/hermes/`.

## Layout

- `tools/hermes_task.py` — one Hermes call (prompt in, text/JSON out)
- `tools/summarize_surf.py` — inventory step 1: per-feature summaries → `inventory/surf/*.json`
- `prompts/` — prompt templates
- `inventory/` — results (`surf/` = features on pepper's live SurfShack master only)
- `patterns.md` — DM → SS14 translation patterns and recorded design decisions

## Status

- [x] SurfShack-side feature summaries (Hermes)
- [x] SS14 base: fork + clone + first Release build (`D:/SS14/Klovnstation14`, ~75 s)
- [x] Compare each feature against Klovn → `inventory/comparison.md`
- [ ] OpenDream compiler hookup for data extraction
- [ ] Agent loop proof of concept on one simple item
