"""Inventory step 1: have Hermes summarise each SurfShack-unique feature.

Reads DM from the SurfShack13 checkout (surfshack13/ modular folder only),
groups files by feature, sends each group to Hermes, and writes
inventory/surf/<feature>.json. Re-running skips features already done.
"""
import concurrent.futures as cf
import json
import os
import sys
from pathlib import Path

from hermes_task import extract_json, run

ROOT = Path(__file__).resolve().parent.parent
# SurfShack13 checkout on pepper's master; override with SURF_DIR
SURF = Path(os.environ.get("SURF_DIR", r"C:\Users\bert\Desktop\Vibecoding SS13\SurfShack13")) / "surfshack13"
OUT = ROOT / "inventory" / "surf"
PROMPT = (ROOT / "prompts" / "summarize_feature.md").read_text(encoding="utf-8")
MAX_CHARS = 180_000  # split bigger features into parts
REQUIRED = {"feature", "one_line", "player_facing_mechanics", "complexity", "portability", "port_notes"}

# feature name -> globs relative to surfshack13/
GROUPS = {
    "bloodsuckers": ["code/modules/bloodsuckers/**/*.dm", "code/__DEFINES/bloodsuckers.dm",
                     "code/__DEFINES/traits/bloodsucker*.dm"],
    "nanites": ["nanites/code/**/*.dm", "code/__DEFINES/nanites.dm", "code/_SIGNALS/nanites.dm"],
    "swarmers": ["code/modules/swarmers/*.dm"],
    "voicechat": ["code/modules/voicechat/*.dm"],
    "store": ["store/**/*.dm"],
    "alligator": ["code/modules/mob/living/basic/alligator*.dm"],
    "space_frog": ["code/modules/mob/living/basic/vermin/space_frog.dm"],
    "soda_robot": ["code/modules/mob/living/basic/bots/soda_robot.dm"],
    "battering_ram": ["code/game/objects/items/battering_ram.dm"],
    "execution_sword": ["code/objects/items/execution_sword.dm"],
    "noose": ["code/objects/structures/noose.dm"],
    "wizard_crayon": ["code/objects/items/wizcrayon.dm", "code/objects/decals/wizcrayon_decals.dm"],
    "bowling": ["code/modules/sports/bowling.dm"],
    "clown_props": ["code/modules/clown/clown_props.dm", "code/game/objects/structures/creampie-torium.dm"],
    "improv_tools": ["code/modules/improv_tools.dm"],
    "playdate": ["code/modules/playdate/playdate.dm"],
    "locker_mech": ["code/modules/vehicles/mecha/combat/locker_mech.dm"],
    "public_announce": ["code/game/machinery/computer/public_announce.dm",
                        "code/game/objects/items/circuitboards/computer_circuitboards.dm",
                        "code/modules/research/designs/comp_board_designs.dm"],
    "tweak": ["code/datums/elements/tweak.dm", "code/modules/mob/living/tweak.dm"],
    "frog_ui": ["code/modules/frog_ui/*.dm", "code/__DEFINES/frog_ui.dm"],
    "antag_spawner_and_roles": ["code/modules/antagonists/_common/antag_spawner.dm",
                                "code/__DEFINES/roles.dm", "code/__HELPERS/roundend.dm"],
    "misc_small": ["code/modules/projectiles/guns/magic/wand.dm", "code/modules/clothing/head/hat.dm",
                   "code/modules/uplink/uplink_items/*.dm", "code/objects/items/storage/uplink_kits.dm",
                   "code/modules/reagents/chemistry/recipes/medicine.dm",
                   "code/modules/datums/components/crafting/misc.dm",
                   "code/modules/wikishit/xenobio_manual.dm",
                   "code/modules/mob/living/silicon/ai/ai_actions/request_shell.dm",
                   "code/modules/mob/living/carbon/human/inventory.dm",
                   "code/datums/sprite_accessories.dm"],
    "client_prefs_ooc": ["code/modules/client/**/*.dm"],
}


def collect(globs):
    files = []
    for g in globs:
        files += sorted(p for p in SURF.glob(g) if p.is_file())
    return list(dict.fromkeys(files))


def chunks(files):
    part, size = [], 0
    for f in files:
        text = f"\n// ===== FILE: {f.relative_to(SURF).as_posix()} =====\n" + \
            f.read_text(encoding="utf-8", errors="replace")
        if part and size + len(text) > MAX_CHARS:
            yield "".join(part)
            part, size = [], 0
        part.append(text)
        size += len(text)
    if part:
        yield "".join(part)


def do_feature(name):
    out = OUT / f"{name}.json"
    if out.exists():
        return name, "skip"
    files = collect(GROUPS[name])
    parts = list(chunks(files))
    results = []
    for i, src in enumerate(parts):
        tag = f"{name}" + (f"_part{i + 1}" if len(parts) > 1 else "")
        prompt = PROMPT.replace("{{FEATURE}}", name).replace(
            "{{PART}}", f"part {i + 1} of {len(parts)}" if len(parts) > 1 else "the whole feature"
        ).replace("{{SOURCE}}", src)
        for attempt in range(3):
            data = extract_json(run(prompt, f"{tag}_try{attempt}" if attempt else tag))
            if REQUIRED <= data.keys():
                break
        else:
            raise ValueError(f"{tag}: reply missing {REQUIRED - data.keys()}")
        results.append(data)
    data = results[0] if len(results) == 1 else {"feature": name, "parts": results}
    data["_files"] = [f.relative_to(SURF).as_posix() for f in files]
    data["_dm_lines"] = sum(len(f.read_text(encoding="utf-8", errors="replace").splitlines()) for f in files)
    out.write_text(json.dumps(data, indent=2), encoding="utf-8")
    return name, "ok"


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    names = sys.argv[1:] or list(GROUPS)
    with cf.ThreadPoolExecutor(max_workers=4) as pool:
        for fut in cf.as_completed([pool.submit(do_feature, n) for n in names]):
            try:
                print(*fut.result(), flush=True)
            except Exception as e:  # keep going; failures are logged
                print("FAIL", e, flush=True)
