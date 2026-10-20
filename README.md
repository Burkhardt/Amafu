# Amafu (`amafu`)

Amafu finds locally synchronized cloud-storage roots and writes the shared
`~/.config/RAIkeep.json5` configuration used by RAIkeep command-line tools. It
can also create stable shortcuts under `~/.CloudStorage/`, avoiding vendor-managed
macOS paths such as `~/Library/CloudStorage/GoogleDrive-…/My Drive/`.

Cloud detection is supported on macOS and Linux. Commands do not move cloud data.

## Amafu v4.5.13

On Linux, discovery checks existing `~/.CloudStorage/` shortcuts, valid cloud
roots in `~/.config/RAIkeep.json5`, and `/srv/ServerData/*Data` mounts. This
includes Mzansi's OneDrive, Google Drive, Dropbox, and iCloud Drive mounts.
`amafu init --create-links` can write clean shortcuts into a new configuration.

Latest release notes: [Amafu 4.5.13 release notes](https://github.com/Burkhardt/RAIkeep/blob/main/doc/releases/v4.x/Amafu_RELEASE_NOTES_4.x.md#4513).

## Previous Versions

For release notes and changelogs of earlier releases, see the [Amafu 4.x Release Notes](https://github.com/Burkhardt/RAIkeep/blob/main/doc/releases/v4.x/Amafu_RELEASE_NOTES_4.x.md).

## Installation

The installer handles **Amafu only**. Pass the release number explicitly so the
installed command is reproducible.

### Native binary, without .NET

Installs the matching macOS or Linux native release binary in `/usr/local/bin`.
It requires `curl`, `unzip`, and administrator permission.

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | sudo bash -s -- native 4.5.13
```

### .NET global tool

Installs the Amafu tool for the current user in the normal .NET global-tools
location. Use this when that directory is already on your `PATH`.

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | bash -s -- global 4.5.13
```

### .NET tool in `/usr/local/bin`

Installs the Amafu tool in `/usr/local/bin`, avoiding a per-user tool directory
on `PATH`. It requires .NET and administrator permission.

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | sudo bash -s -- system 4.5.13
```

Do not run `amafu` itself with `sudo`; it should use the ordinary user's cloud
folders and configuration.

## Commands

### `detect`

Inspect the cloud roots Amafu finds without changing the machine:

```bash
amafu detect
```

On Ubuntu, preview the mounted roots as JSON without writing anything:

```bash
amafu detect --json --dry-run
```

Use `--json` when another program needs the result. With more than one personal
OneDrive root, select one deterministically for this run:

```bash
amafu detect --onedrive-personal 'OneDrive-Personal(2)' --json
```

### `init`

For a new machine, create clean shortcuts and the initial configuration:

```bash
amafu init --create-links
```

Preview the exact result first with `--dry-run`. `init` preserves an existing
configuration unless `--force` is supplied.

### `reconcile`

For an existing configuration, preview additional detected roots and any move
to shortcut paths:

```bash
amafu reconcile
```

Apply the preview only after review:

```bash
amafu reconcile --apply
```

Amafu creates a backup before changing an existing configuration. It does not
move or delete provider data.

## Terminal Font

The `amafu` help screen uses Nerd Font glyphs. The tested choice for Blink on
iPadOS is the [JetBrains Mono Nerd Font stylesheet](https://github.com/blinksh/patched-fonts/blob/main/Jet%20Brains%20Mono%20Nerd%20Font.css). The [terminal font guide](https://github.com/Burkhardt/RAIkeep/blob/main/doc/TERMINAL_FONTS.md) also covers macOS and Ubuntu.
