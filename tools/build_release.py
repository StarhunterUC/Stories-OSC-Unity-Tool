#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
import argparse
import hashlib
import json
import shutil
import tarfile
import zipfile


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_checksum(path: Path) -> Path:
    checksum = path.with_name(path.name + ".sha256")
    checksum.write_text(
        f"{sha256(path)}  {path.name}\n",
        encoding="ascii",
    )
    return checksum


def add_unitypackage_entry(
    archive: tarfile.TarFile,
    guid: str,
    pathname: str,
    asset: bytes | None,
    meta: bytes,
) -> None:
    def add_bytes(name: str, payload: bytes) -> None:
        info = tarfile.TarInfo(name)
        info.size = len(payload)
        info.mode = 0o644
        archive.addfile(info, fileobj=__import__("io").BytesIO(payload))

    add_bytes(f"{guid}/pathname", (pathname + "\n").encode("utf-8"))
    if asset is not None:
        add_bytes(f"{guid}/asset", asset)
    add_bytes(f"{guid}/asset.meta", meta)


def build_unitypackage(source: Path, output: Path, asset_path: str) -> None:
    script_guid = hashlib.sha256(asset_path.encode("utf-8")).hexdigest()[:32]
    parent_path = str(Path(asset_path).parent).replace("\\", "/")
    grandparent_path = str(Path(parent_path).parent).replace("\\", "/")
    parent_guid = hashlib.sha256(parent_path.encode("utf-8")).hexdigest()[:32]
    grandparent_guid = hashlib.sha256(grandparent_path.encode("utf-8")).hexdigest()[:32]

    folder_meta = lambda guid: (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    ).encode("utf-8")

    script_meta = (
        "fileFormatVersion: 2\n"
        f"guid: {script_guid}\n"
        "MonoImporter:\n"
        "  externalObjects: {}\n"
        "  serializedVersion: 2\n"
        "  defaultReferences: []\n"
        "  executionOrder: 0\n"
        "  icon: {instanceID: 0}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    ).encode("utf-8")

    with tarfile.open(output, "w:gz", format=tarfile.GNU_FORMAT) as archive:
        add_unitypackage_entry(
            archive,
            grandparent_guid,
            grandparent_path,
            None,
            folder_meta(grandparent_guid),
        )
        add_unitypackage_entry(
            archive,
            parent_guid,
            parent_path,
            None,
            folder_meta(parent_guid),
        )
        add_unitypackage_entry(
            archive,
            script_guid,
            asset_path,
            source.read_bytes(),
            script_meta,
        )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", default=".")
    args = parser.parse_args()

    root = Path(args.repo_root).resolve()
    version = json.loads((root / "version.json").read_text(encoding="utf-8"))
    tag = version["tag"]
    source_name = version["canonical_source_asset"]
    source = root / source_name
    if not source.is_file():
        raise FileNotFoundError(source)

    dist = root / "dist" / tag
    if dist.exists():
        shutil.rmtree(dist)
    dist.mkdir(parents=True)

    standalone = dist / source_name
    shutil.copy2(source, standalone)
    write_checksum(standalone)

    unitypackage = dist / f"Stories-OSC-Unity-Tool-{tag}.unitypackage"
    build_unitypackage(source, unitypackage, version["unity_asset_path"])
    write_checksum(unitypackage)

    manual_zip = dist / f"Stories-OSC-Unity-Tool-{tag}.zip"
    with zipfile.ZipFile(
        manual_zip,
        "w",
        compression=zipfile.ZIP_DEFLATED,
        compresslevel=9,
    ) as archive:
        archive.write(
            source,
            version["unity_asset_path"],
        )
        for relative in (
            "README.md",
            "CHANGELOG.md",
            f"RELEASE_NOTES_{tag}.md",
            f"RAYCAST_GUIDE_{tag}.md",
            f"SOURCE_AUDIT_{tag}.json",
            f"TEST_PLAN_{tag}.md",
            "OSC_PROTOCOL_v19.md",
            "version.json",
        ):
            path = root / relative
            if path.is_file():
                archive.write(path, relative)
        for directory in ("contracts", "registries"):
            for path in sorted((root / directory).glob("**/*")):
                if path.is_file():
                    archive.write(path, path.relative_to(root).as_posix())
    write_checksum(manual_zip)

    notes = root / f"RELEASE_NOTES_{tag}.md"
    if notes.is_file():
        shutil.copy2(notes, dist / notes.name)

    manifest = []
    for path in sorted(dist.iterdir()):
        if path.is_file() and path.name != "SHA256SUMS.txt":
            manifest.append(f"{sha256(path)}  {path.name}")
    (dist / "SHA256SUMS.txt").write_text(
        "\n".join(manifest) + "\n",
        encoding="ascii",
    )

    print(f"Release assets built: {dist}")
    for path in sorted(dist.iterdir()):
        if path.is_file():
            print(f" - {path.name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
