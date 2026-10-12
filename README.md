# Amafu (`amafu`)

Amafu finds locally synchronized cloud-storage roots and writes the shared
`~/.config/RAIkeep.json5` configuration used by RAIkeep command-line tools. On
macOS, it can also create stable shortcuts under `~/.CloudStorage/`, avoiding
vendor-managed paths such as `~/Library/CloudStorage/GoogleDrive-…/My Drive/`.

Cloud detection is supported on macOS. Commands do not move cloud data.

## Amafu v4.5.8

This release prefers existing `~/.CloudStorage` shortcuts when creating a
configuration. `amafu init --create-links` creates missing shortcuts and writes
those clean paths into a new configuration.

Latest release notes: [Amafu_RELEASE_NOTES_4.5.8.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.8.md).

<details>
<summary><b>Previous Versions</b></summary>

### Amafu v4.5.7

Coordinated with the RAIkeep 4.5.7 release line; no Amafu behavior changed.

Release notes: [Amafu_RELEASE_NOTES_4.5.7.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.7.md).

### Amafu v4.5.6

Coordinated with the RAIkeep 4.5.6 release line.

Release notes: [Amafu_RELEASE_NOTES_4.5.6.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.6.md).

### Amafu v4.5.5

Coordinated package and dependency alignment; no independent Amafu behavior changed.

Release notes: [Amafu_RELEASE_NOTES_4.5.5.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.5.md).

### Amafu v4.5.4

`amafu init --create-links` began writing the shortcuts it creates into a new
configuration. Dry runs show the same paths without writing files or links.

Release notes: [Amafu_RELEASE_NOTES_4.5.4.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.4.md).

### Amafu v4.5.3

Added multiple Google Drive and corporate OneDrive accounts, named shortcuts,
deterministic personal OneDrive selection, and noninteractive reconciliation.

Release notes: [Amafu_RELEASE_NOTES_4.5.3.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.3.md).

### Amafu v4.5.2

Added optional `--create-links` shortcuts under `~/.CloudStorage/` for
detection and initial configuration.

Release notes: [Amafu_RELEASE_NOTES_4.5.2.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.2.md).

### Amafu v4.5.0

Coordinated release; cloud-configuration behavior did not change.

Release notes: [Amafu_RELEASE_NOTES_4.5.0.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.0.md).

### Amafu v4.4.8

Coordinated release; cloud-configuration behavior did not change.

Release notes: [Amafu_RELEASE_NOTES_4.4.8.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.4.8.md).

### Amafu v4.4.6

Participated in the synchronized CR049 release; cloud-configuration behavior
did not change.

Release notes: [Amafu_RELEASE_NOTES_4.4.6.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.4.6.md).

</details>

## Installation

The installer handles **Amafu only**. Pass the release number explicitly so the
installed command is reproducible.

### Native binary, without .NET

Installs the matching macOS or Linux native release binary in `/usr/local/bin`.
It requires `curl`, `unzip`, and administrator permission.

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | sudo bash -s -- native 4.5.8
```

### .NET global tool

Installs the Amafu tool for the current user in the normal .NET global-tools
location. Use this when that directory is already on your `PATH`.

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | bash -s -- global 4.5.8
```

### .NET tool in `/usr/local/bin`

Installs the Amafu tool in `/usr/local/bin`, avoiding a per-user tool directory
on `PATH`. It requires .NET and administrator permission.

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | sudo bash -s -- system 4.5.8
```

Do not run `amafu` itself with `sudo`; it should use the ordinary user's cloud
folders and configuration.

## Commands

### `detect`

Inspect the cloud roots Amafu finds without changing the machine:

```bash
amafu detect
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
