"""Convert states from a BYOND .dmi into an SS14 .rsi folder.

    python dmi2rsi.py SRC.dmi OUT.rsi --copyright "..." [--states a=b c ...] [--license CC-BY-SA-3.0]

`--states` picks states to copy; `old=new` renames. With none given, every state is copied.
An existing meta.json in OUT.rsi is merged into (states replaced by name), so several DMIs
can feed one RSI (e.g. icon + lefthand + righthand).
"""
import argparse
import json
import math
import re
from pathlib import Path

from PIL import Image

DIR_COUNTS = {1: 1, 4: 4, 8: 8}


def parse_dmi(path: Path):
    img = Image.open(path)
    desc = img.info.get("Description")
    if not desc:
        raise ValueError(f"{path}: no DMI metadata")
    width = int(re.search(r"width = (\d+)", desc).group(1))
    height = int(re.search(r"height = (\d+)", desc).group(1))
    states, cur = [], None
    for line in desc.splitlines():
        line = line.strip()
        if line.startswith("state = "):
            cur = {"name": json.loads(line[8:]), "dirs": 1, "frames": 1, "delays": None}
            states.append(cur)
        elif cur and " = " in line:
            key, val = line.split(" = ", 1)
            if key in ("dirs", "frames"):
                cur[key] = int(val)
            elif key == "delays":
                cur["delays"] = [float(v) for v in val.split(",")]
    return img.convert("RGBA"), width, height, states


def extract(img, width, height, states):
    """Yield (state, frames[dir][frame] -> Image) in DMI order (frame-major, dirs inside)."""
    cols = img.width // width
    index = 0
    for st in states:
        grid = [[None] * st["frames"] for _ in range(st["dirs"])]
        for f in range(st["frames"]):
            for d in range(st["dirs"]):
                x, y = (index % cols) * width, (index // cols) * height
                grid[d][f] = img.crop((x, y, x + width, y + height))
                index += 1
        yield st, grid


def write_state(out: Path, name, grid, width, height):
    frames = [im for row in grid for im in row]  # RSI order: dir-major
    cols = min(len(frames), 8) if len(frames) > 1 else 1
    rows = math.ceil(len(frames) / cols)
    sheet = Image.new("RGBA", (cols * width, rows * height))
    for i, im in enumerate(frames):
        sheet.paste(im, ((i % cols) * width, (i // cols) * height))
    sheet.save(out / f"{name}.png")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("dmi", type=Path)
    ap.add_argument("rsi", type=Path)
    ap.add_argument("--copyright", required=True)
    ap.add_argument("--license", default="CC-BY-SA-3.0")
    ap.add_argument("--states", nargs="*", default=None)
    args = ap.parse_args()

    wanted = None
    if args.states:
        wanted = dict(s.split("=", 1) if "=" in s else (s, s) for s in args.states)

    img, width, height, states = parse_dmi(args.dmi)
    args.rsi.mkdir(parents=True, exist_ok=True)
    meta_path = args.rsi / "meta.json"
    meta = json.loads(meta_path.read_text()) if meta_path.exists() else {
        "version": 1, "license": args.license, "copyright": args.copyright,
        "size": {"x": width, "y": height}, "states": []}
    if (meta["size"]["x"], meta["size"]["y"]) != (width, height):
        raise ValueError(f"{args.dmi}: {width}x{height} does not match RSI size {meta['size']}")

    found = set()
    for st, grid in extract(img, width, height, states):
        if wanted is not None and st["name"] not in wanted:
            continue
        name = wanted[st["name"]] if wanted else st["name"]
        found.add(st["name"])
        write_state(args.rsi, name, grid, width, height)
        entry = {"name": name}
        if st["dirs"] > 1:
            entry["directions"] = st["dirs"]
        if st["frames"] > 1:
            delays = [d / 10 for d in (st["delays"] or [1] * st["frames"])]  # ticks -> seconds
            entry["delays"] = [delays for _ in range(st["dirs"])]
        meta["states"] = [s for s in meta["states"] if s["name"] != name] + [entry]

    if wanted is not None and set(wanted) - found:
        raise ValueError(f"states not found in {args.dmi}: {sorted(set(wanted) - found)}")
    meta_path.write_text(json.dumps(meta, indent=4) + "\n")
    print(f"{args.rsi}: {sorted(found)}")


if __name__ == "__main__":
    main()
