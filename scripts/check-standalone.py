"""Verify the restored Amafu application has no RAIkeep build or runtime dependencies."""
import json
from pathlib import Path


def main():
    repo = Path(__file__).resolve().parents[1]
    assets = json.loads((repo / "amafu/obj/project.assets.json").read_text())
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
    resolved = {key.split("/", 1)[0] for key in assets["libraries"]}
    for framework in assets["project"]["frameworks"].values():
        resolved.update(dep["name"] for dep in framework.get("downloadDependencies", []))
    forbidden = sorted(name for name in resolved if name.lower() in suite)
    if forbidden:
        raise SystemExit("Amafu depends on RAIkeep components: " + ", ".join(forbidden))
    print("Amafu standalone dependency check passed.")


if __name__ == "__main__":
    main()
