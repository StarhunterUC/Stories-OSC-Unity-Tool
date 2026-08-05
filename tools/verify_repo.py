#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
import argparse
import hashlib
import json
import re


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", default=".")
    args = parser.parse_args()

    root = Path(args.repo_root).resolve()
    version = json.loads((root / "version.json").read_text(encoding="utf-8"))
    source = root / version["canonical_source_asset"]
    text = source.read_text(encoding="utf-8-sig")

    checks = {
        "version": f'Version = "{version["version"]}"' in text,
        "build": f'BuildNumber = "{version["build_number"]}"' in text,
        "repository": version["repository"] in text,
        "class": "class StoriesOfYggdrasilOSCContactSystem" in text,
        "editor_guard": text.count("#if UNITY_EDITOR") == 1 and text.count("#endif") == 1,
        "spell_bus_fix": "int.TryParse(suffix, out spellId)" in text,
        "current_spell_bus": "tag == SpellActiveTag" in text and "SpellBitTagPrefix" in text,
        "sha256_updater": 'EndsWith(".sha256"' in text,
        "managed_repair": "Managed-System Repair Center" in text,
    }
    failed = [name for name, passed in checks.items() if not passed]
    if failed:
        raise SystemExit("Verification failed: " + ", ".join(failed))

    actual = hashlib.sha256(source.read_bytes()).hexdigest()
    expected = version["source_sha256"]
    if actual != expected:
        raise SystemExit(
            f"Source SHA mismatch: expected {expected}, received {actual}"
        )

    print("VERIFY PASSED")
    for name in checks:
        print(f" - {name}: OK")
    print(f" - source_sha256: {actual}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
