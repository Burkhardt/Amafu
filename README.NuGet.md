# Amafu (`amafu`)

Amafu finds locally synchronized cloud-storage roots and writes the shared
`~/.config/RAIkeep.json5` configuration used by RAIkeep command-line tools. On
macOS, it can create stable shortcuts under `~/.CloudStorage/` for cloud paths
managed by the storage providers.

Cloud detection is supported on macOS. Commands do not move cloud data.

## Amafu v4.5.8

This release prefers existing `~/.CloudStorage` shortcuts when creating a
configuration. `amafu init --create-links` creates missing shortcuts and writes
those paths into a new configuration.

Latest release notes: [Amafu_RELEASE_NOTES_4.5.8.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.8.md).

## Previous Versions

[Read the earlier release summaries in the Amafu README on GitHub](https://github.com/Burkhardt/Amafu/blob/main/README.md). Its history is folded under “Previous Versions.”

## Installation

The installer handles Amafu only. Pass the release number explicitly.

### Native binary, without .NET

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | sudo bash -s -- native 4.5.8
```

### .NET global tool

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | bash -s -- global 4.5.8
```

### .NET tool in `/usr/local/bin`

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | sudo bash -s -- system 4.5.8
```

Do not run `amafu` itself with `sudo`; it should use the ordinary user's cloud
folders and configuration.

## Commands

Detect available cloud roots:

```bash
amafu detect
```

Create shortcuts and an initial configuration:

```bash
amafu init --create-links
```

Preview changes to an existing configuration, then apply them after review:

```bash
amafu reconcile
amafu reconcile --apply
```

For `--dry-run`, `--json`, and account selection, see the [full Amafu README](https://github.com/Burkhardt/Amafu/blob/main/README.md).

## Terminal Font

The `amafu` help screen uses Nerd Font glyphs. The [terminal font guide](https://github.com/Burkhardt/RAIkeep/blob/main/doc/TERMINAL_FONTS.md) covers macOS, Ubuntu, and Blink on iPadOS.
