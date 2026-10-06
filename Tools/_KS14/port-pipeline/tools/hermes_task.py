"""Run one sandboxed Hermes task: prompt text in, text (usually JSON) out.

Hermes runs with only the harmless `todo` toolset and --ignore-rules, so it
cannot read/write files or run shell commands. All source code it needs is
inlined into the query file by the caller.
"""
import json
import re
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LOGS = ROOT / "logs" / "hermes"


def run(prompt: str, name: str, timeout: int = 600, retries: int = 1) -> str:
    LOGS.mkdir(parents=True, exist_ok=True)
    qfile = LOGS / f"{name}.query.txt"
    qfile.write_text(prompt, encoding="utf-8")
    last_err = ""
    for attempt in range(retries + 1):
        start = time.time()
        proc = subprocess.run(
            ["hermes", "chat", "--query-file", str(qfile), "-Q", "--oneshot",
             "-t", "todo", "--ignore-rules"],
            capture_output=True, text=True, encoding="utf-8", errors="replace",
            timeout=timeout,
        )
        out = re.sub(r"\n*session_id: \S+\s*$", "", proc.stdout).strip()
        (LOGS / f"{name}.out.txt").write_text(
            f"# rc={proc.returncode} attempt={attempt} {time.time() - start:.0f}s\n"
            f"{out}\n\n# stderr\n{proc.stderr}", encoding="utf-8")
        if proc.returncode == 0 and out:
            return out
        last_err = proc.stderr[-500:]
    raise RuntimeError(f"hermes failed for {name}: {last_err}")


def extract_json(text: str):
    """Pull the first JSON object/array out of a model reply."""
    fence = re.search(r"```(?:json)?\s*(.*?)```", text, re.S)
    if fence:
        text = fence.group(1)
    start = min((i for i in (text.find("{"), text.find("[")) if i != -1), default=-1)
    if start == -1:
        raise ValueError("no JSON in reply")
    return json.JSONDecoder().raw_decode(text[start:])[0]


if __name__ == "__main__":
    print(run(Path(sys.argv[1]).read_text(encoding="utf-8"), sys.argv[2]))
