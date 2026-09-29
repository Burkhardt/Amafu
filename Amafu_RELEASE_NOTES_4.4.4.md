# Release Notes: Amafu v4.4.4

Amafu v4.4.4 is the inaugural release of the standalone RAIkeep cloud-storage
discovery and configuration bootstrap CLI.

It implements the accepted provider amendment in
`CR044_AIA_and_jsonpit_to_RAIkeep_Auto-Detect-Cloud-Drives-and-Init-Config.md`.

## Delivered

- Adds the `amafu detect` and `amafu detect --json` read-only discovery commands.
- Adds `amafu init`, its `amafu init-config` alias, `--dry-run`, and explicit
  `--force` regeneration.
- Detects documented OneDrive, Dropbox, Google Drive, and iCloud Drive locations
  on macOS with deterministic precedence and inner data-directory selection.
- Generates the shared `~/.config/RAIkeep.json5` without referencing or mutating
  OsLib's `Os.Config`.
- Rejects `sudo`/root execution and applies Unix mode `0644`.
- Provides the aligned RAIkeep Nerd Font help experience.
- Publishes a NuGet global tool and self-contained NativeAOT artifacts for macOS,
  Linux, and Windows with SHA-256 checksums.
- Remains free of RAIkeep and third-party runtime dependencies.

Preparation stops before tagging and publication for RAI's manual synchronized
release-chain execution.
