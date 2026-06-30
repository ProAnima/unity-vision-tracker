#!/usr/bin/env python3
"""Validate the embedded UPM package layout and release policy."""

from __future__ import annotations

import json
import pathlib
import sys


PACKAGE_NAME = "com.proanima.universal-vision-tracker"
INSTALL_URL = f"https://github.com/ProAnima/unity-vision-tracker.git?path=/Packages/{PACKAGE_NAME}"
EXPECTED_PACKAGE_FIELDS = {
    "name": PACKAGE_NAME,
    "unity": "6000.0",
    "license": "MIT",
}
EXPECTED_DEPENDENCIES = {
    "com.unity.ai.inference": "2.6.1",
    "com.unity.ugui": "2.0.0",
}
FORBIDDEN_MODEL_SUFFIXES = {
    ".onnx",
    ".sentis",
    ".tflite",
    ".pt",
    ".pth",
    ".weights",
    ".engine",
}


def main() -> int:
    root = pathlib.Path(__file__).resolve().parents[1]
    package_root = root / "Packages" / PACKAGE_NAME
    manifest_path = package_root / "package.json"
    errors: list[str] = []

    manifest = load_manifest(manifest_path, errors)
    if manifest:
        validate_manifest(manifest, package_root, errors)
        validate_release_metadata(root, package_root, manifest, errors)

    validate_required_paths(root, package_root, errors)
    validate_install_docs(root, package_root, errors)
    validate_forbidden_model_weights(root, package_root, errors)

    if errors:
        for error in errors:
            print(f"::error::{error}")
        return 1

    print("UPM package layout validated.")
    return 0


def load_manifest(manifest_path: pathlib.Path, errors: list[str]) -> dict | None:
    if not manifest_path.exists():
        errors.append("Missing package.json")
        return None

    try:
        return json.loads(manifest_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        errors.append(f"package.json is not valid JSON: {exc}")
        return None


def validate_manifest(manifest: dict, package_root: pathlib.Path, errors: list[str]) -> None:
    for key, value in EXPECTED_PACKAGE_FIELDS.items():
        if manifest.get(key) != value:
            errors.append(f"package.json {key!r} must be {value!r}, got {manifest.get(key)!r}")

    dependencies = manifest.get("dependencies", {})
    for name, version in EXPECTED_DEPENDENCIES.items():
        if dependencies.get(name) != version:
            errors.append(f"{name} must stay pinned to {version}")

    samples = manifest.get("samples", [])
    if not samples:
        errors.append("package.json must declare importable samples")

    for sample in samples:
        sample_path = package_root / sample.get("path", "")
        if not sample_path.exists():
            errors.append(f"Sample path does not exist: {sample.get('path')}")
        if not sample.get("displayName"):
            errors.append(f"Sample is missing displayName: {sample}")


def validate_required_paths(root: pathlib.Path, package_root: pathlib.Path, errors: list[str]) -> None:
    required_paths = [
        package_root / "Runtime" / "UniversalTracker.Runtime.asmdef",
        package_root / "Editor" / "UniversalTracker.Editor.asmdef",
        package_root / "Documentation~" / "GETTING_STARTED.md",
        package_root / "Documentation~" / "ARCHITECTURE_ROADMAP.md",
        package_root / "Samples~" / "YOLO Model Profiles" / "README.md",
        package_root / "CHANGELOG.md",
        root / "README.md",
        root / "LICENSE",
        root / "CODEX.md",
        root / "TESTING.md",
        root / "RELEASE.md",
    ]

    for path in required_paths:
        if not path.exists():
            errors.append(f"Missing required path: {path.relative_to(root)}")


def validate_release_metadata(root: pathlib.Path, package_root: pathlib.Path, manifest: dict, errors: list[str]) -> None:
    version = manifest.get("version")
    changelog_path = package_root / "CHANGELOG.md"
    if not version:
        errors.append("package.json must declare a version")
        return

    if changelog_path.exists():
        changelog = changelog_path.read_text(encoding="utf-8")
        if f"## {version}" not in changelog:
            errors.append(f"CHANGELOG.md must contain an entry for package version {version}")

    release_doc = root / "RELEASE.md"
    if release_doc.exists():
        release_text = release_doc.read_text(encoding="utf-8").replace("`", "").lower()
        if "package.json version matches the release tag" not in release_text:
            errors.append("RELEASE.md must include the package version release gate")


def validate_install_docs(root: pathlib.Path, package_root: pathlib.Path, errors: list[str]) -> None:
    docs = [
        root / "README.md",
        package_root / "README.md",
    ]
    for doc in docs:
        if doc.exists() and INSTALL_URL not in doc.read_text(encoding="utf-8"):
            errors.append(f"Install URL is missing from {doc.relative_to(root)}")


def validate_forbidden_model_weights(root: pathlib.Path, package_root: pathlib.Path, errors: list[str]) -> None:
    for path in package_root.rglob("*"):
        if path.is_file() and path.suffix.lower() in FORBIDDEN_MODEL_SUFFIXES:
            errors.append(f"Model weight file must not be bundled in the package: {path.relative_to(root)}")


if __name__ == "__main__":
    sys.exit(main())
