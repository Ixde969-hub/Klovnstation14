# SurfShack → Klovnstation14: what exists, what to port

Source: pepper's live SurfShack (`peppyrmynt/SurfShack13` master, `surfshack13/` modular folder, ~17k DM lines).
Target: `Space-Klovns/Klovnstation14` master @ ed978757a2 (2026-10-05). Per-feature details: `surf/<feature>.json` (Hermes).

Verdicts: **skip** (BYOND-only or already in Klovn) · **decide** (needs a design call from you first) ·
**port** (S / M / L effort).

| Feature | DM lines | Klovn already has | Verdict |
|---|---|---|---|
| voicechat | 663 | Yes — `_KS14/Voice` (browser-linked proximity voice, moderation, PTT) | **skip** |
| playdate | 35 | n/a — BYOND hub status text | **skip** |
| frog_ui | ~150 | n/a — BYOND UI theme | **skip** |
| client_prefs_ooc (country flags) | 116 | No — needs IP geolocation | **skip** (flags) / see store |
| store + doubloons + antag_spawner | ~870 | No metacurrency, no DB tables for it | **decide** — server-wide economy, needs DB migrations; Klovn maintainers may not want it |
| battering_ram | 139 | No | **port S** — proof-of-concept candidate |
| improv_tools | 110 | Partly — makeshift weapons/cuffs/shield, no makeshift tools | **port S** |
| noose | 155 | No | **port S** (check Klovn content rules) |
| tweak (emote + wand) | 98 + wand | No | **port S** |
| clown_props (banana-copter, creampie-torium) | 128 | Pie launcher only | **port S** |
| misc_small (gator cloak, uplink entries, hairstyle, recipes) | 176 | Uplink exists | **port S** — mostly data |
| alligator | 389 | No | **port M** — HTN NPC + death-roll grab |
| space_frog (6 frog variants) | 172 | Plain `MobFrog` only | **port M** |
| soda_robot | 99 | No | **port M** |
| bowling | 160 | No | **port M** |
| execution_sword | 122 | Gun executions (`_KS14/Execution`) — different mechanic | **port M** — can reuse execution plumbing |
| wizard_crayon (magic decals) | 246 | Crayons yes, magic stencils no | **port M** |
| locker_mech | 143 | Mechs + `_KS14/Mech` | **port M** |
| public_announce (paid crew announcement) | 123 | Comms console only | **port M** |
| swarmers (antag) | 862 | No | **port L** |
| bloodsuckers (vampire antag) | 6355 | No (only animal "bloodsucker" organs) | **port L** — biggest; many design calls |
| nanites | 6844 | No | **port L** — whole research/program system |

## Rough totals

- **Skip:** 4 features (~960 lines) — BYOND-only or Klovn already has it.
- **Decide first:** metacurrency store (~870 lines).
- **Port:** 17 features — 6 small, 8 medium, 3 large (bloodsuckers, nanites, swarmers ≈ 80% of the remaining code).

## Notes

- Klovn conventions (`CONTRIBUTING.md`): new code under `_KS14/`, `// KS14:` markers on upstream edits,
  `Ks` prefix on collision-prone IDs, build `-c Release` + test `-c Debug`, attribute subscriptions.
- Licensing: Surf code is AGPL-3.0, Klovn is MIT. Ports are reimplementations against SS14 APIs, not code copies.
  Sprites/sounds (CC-BY-SA 3.0 from /tg/ lineage) need attribution in `meta.json` / `attributions.yml`.
- Klovn's `_KsModule*` submodules are private; we build without them. Anything that depends on them is invisible to us.
