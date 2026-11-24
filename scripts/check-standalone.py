"""Verify the restored Amafu application has no RAIkeep build or runtime dependencies."""
import argparse
import json
from pathlib import Path


def main():
    repo = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--include-tests", action="store_true")
    args = parser.parse_args()
    # The release workflow also runs in an independent checkout of this repository.
    suite = set("OsLib OsLibCore RaiUtils RaiImage RaiDiagram JsonPit ImgSeeder PitSeeder "
                "RaidSeeder RaiGuard RaiGuard.Analyzer RaiGuard.Core RaiGuard.CodeFixes "
                "inqaba umgodi-json vikela".lower().split())
    manifest = repo.parent / "scripts/release-manifest.json"
    if manifest.is_file():
        for package in json.loads(manifest.read_text())["packages"]:
            if package["name"] != "Amafu":
                suite.update(name.lower() for name in
                             [package["name"], package["packageId"], *package.get("extraPackages", [])])
    projects = ["amafu", "amafu.Tests"] if args.include_tests else ["amafu"]
    for project in projects:
        assets = json.loads((repo / project / "obj/project.assets.json").read_text())
        resolved = {key.split("/", 1)[0] for key in assets["libraries"]}
        for framework in assets["project"]["frameworks"].values():
            resolved.update(dep["name"] for dep in framework.get("downloadDependencies", []))
        forbidden = sorted(name for name in resolved if name.lower() in suite)
        if forbidden:
            raise SystemExit(f"{project} depends on RAIkeep components: " + ", ".join(forbidden))
    print("Amafu standalone dependency check passed.")


if __name__ == "__main__":
    main()
