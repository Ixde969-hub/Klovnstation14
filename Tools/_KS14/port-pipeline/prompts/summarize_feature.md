You are analysing Space Station 13 code (BYOND DreamMaker, a /tg/station-based fork called SurfShack) so it can later be ported to Space Station 14 (C#, RobustToolbox ECS, YAML prototypes).

Feature: {{FEATURE}} — you are seeing {{PART}}.

Read the DM source below and reply with ONLY a JSON object (no prose, no markdown fences) with exactly these keys:

{
  "feature": "<name>",
  "one_line": "<what it is, for a player, one sentence>",
  "player_facing_mechanics": ["<each distinct thing a player can see or do>"],
  "content_types": ["<item | structure | machine | mob | antagonist | game_mode_ruleset | reagent | recipe | ui | admin_tool | infrastructure | other>"],
  "type_paths": ["<main /obj, /mob, /datum paths defined>"],
  "tg_systems_used": ["<SS13 systems it depends on, e.g. dynamic rulesets, antag datums, mood, research nodes, basic mob AI, components/elements, signals, TGUI, subsystems, uplink>"],
  "data_only_parts": ["<parts that are pure data/stat overrides, convertible to YAML without logic>"],
  "logic_parts": ["<parts needing real behaviour code>"],
  "byond_specific": ["<anything tied to BYOND itself: verbs, client procs, browse(), external servers, topic calls>"],
  "complexity": "<data | simple | medium | large>",
  "portability": "<yes | partial | infrastructure_only>",
  "port_notes": "<2-4 sentences: what an SS14 port would need, which SS14 concepts map (components, systems, events, DoAfter, actions, gamerules), and design questions a human must answer>"
}

Be concrete and base everything on the code. Do not invent features that are not in the source.

SOURCE:
{{SOURCE}}
