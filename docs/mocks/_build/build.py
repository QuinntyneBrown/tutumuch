#!/usr/bin/env python3
"""Render every TuTuMuch mock from the screen modules.

    python docs/mocks/_build/build.py                 # everything + manifest.json
    python docs/mocks/_build/build.py --only commerce # one module (screens_commerce.py); no manifest

A partial build skips modules that fail to import (another author may be
mid-edit) and only warns about links into screens it did not load. A full
build fails on any import error, missing renderer or broken mock link.
Then run the skill checker:  python .claude/skills/writing-html-mocks/scripts/check_mocks.py docs/mocks --write
"""
from __future__ import annotations

import argparse
import importlib
import json
import sys
import traceback
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
sys.path.insert(0, str(HERE))

import mk  # noqa: E402

REQUIRED = {"page": ["default", "loading", "empty", "error"], "dialog": ["default", "busy", "invalid"], "notification": ["info", "success", "warning", "danger"]}


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", help="module suffix, e.g. commerce for screens_commerce.py")
    args = ap.parse_args()
    modules = sorted(p.stem for p in HERE.glob("screens_*.py"))
    failed = []
    for m in modules:
        try:
            importlib.import_module(m)
        except Exception:
            if args.only and m != f"screens_{args.only}":
                failed.append(m)
                print(f"skip {m}: import failed (partial build)", file=sys.stderr)
                continue
            traceback.print_exc()
            return 1
    target = f"screens_{args.only}" if args.only else None
    errors: list[str] = []
    count = 0
    for s in mk.REGISTRY.values():
        for st in REQUIRED[s.kind]:
            if st not in s.states and st not in s.na:
                errors.append(f"{s.kind} {s.id}: missing required state '{st}' (add it or declare na={{'{st}': 'reason'}})")
        if target and s.module != target:
            continue
        out_dir = ROOT / mk.KIND_DIR[s.kind] / s.id
        out_dir.mkdir(parents=True, exist_ok=True)
        for st in s.states:
            try:
                html = mk.render(s, st)
            except Exception:
                errors.append(f"{s.id}/{st}: render failed\n{traceback.format_exc()}")
                continue
            (out_dir / f"{st}.html").write_text(html, encoding="utf-8")
            count += 1
        # remove stale state files for this screen
        for f in out_dir.glob("*.html"):
            if f.stem not in s.states:
                f.unlink()
    # link validation
    for kind, sid, st in mk.LINKS:
        s = mk.REGISTRY.get(sid)
        if not s:
            msg = f"link to unknown {kind} '{sid}'"
            (print("warn:", msg, file=sys.stderr) if args.only else errors.append(msg))
            continue
        if s.kind != kind or st not in s.states:
            errors.append(f"link to {kind} '{sid}' state '{st}' which does not exist")
    if not args.only:
        manifest = {
            "project": "TuTuMuch",
            "required_states": REQUIRED,
            "screens": [
                {k: v for k, v in {
                    "id": s.id, "kind": s.kind, "title": s.title, "route": s.route, "opens_from": s.opens_from,
                    "requirements": s.reqs, "states": list(s.states), "not_applicable": s.na or None,
                }.items() if v}
                for s in sorted(mk.REGISTRY.values(), key=lambda s: (list(mk.KIND_DIR).index(s.kind), s.module, s.id))
            ],
        }
        (ROOT / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    for e in sorted(set(errors)):
        print("error:", e, file=sys.stderr)
    print(f"wrote {count} mocks from {len(mk.REGISTRY)} screens" + (f" (skipped {failed})" if failed else ""))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
