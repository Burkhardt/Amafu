# Amafu (`amafu`)

Amafu finds locally synchronized cloud-storage roots and writes the shared
`~/.config/RAIkeep.json5` configuration used by RAIkeep command-line tools. It
can also create stable shortcuts under `~/.CloudStorage/`, avoiding vendor-managed
macOS paths such as `~/Library/CloudStorage/GoogleDrive-…/My Drive/`.

Cloud detection is supported on macOS and Linux. Commands do not move cloud data.

## Amafu v4.5.17

Read configuration paths, provider order, and observer targets with the new
`list` selectors. Named lookups print a single value; collection views use the
shared provider badges and colors. Each selector has its own help screen.
The native binary remains independent of a .NET runtime.

Latest release notes: [Amafu 4.5.17 release notes](https://github.com/Burkhardt/RAIkeep/blob/main/doc/releases/v4.x/Amafu_RELEASE_NOTES_4.x.md#4517).

## Previous Versions

For release notes and changelogs of earlier releases, see the [Amafu 4.x Release Notes](https://github.com/Burkhardt/RAIkeep/blob/main/doc/releases/v4.x/Amafu_RELEASE_NOTES_4.x.md).

## Installation

The installer handles **Amafu only**. Pass the release number explicitly so the
installed command is reproducible.

### Native binary, without .NET

Installs the matching macOS or Linux native release binary in `/usr/local/bin`.
It requires `curl`, `unzip`, and administrator permission.

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | sudo bash -s -- native 4.5.17
```

### .NET global tool

Installs the Amafu tool for the current user in the normal .NET global-tools
location. Use this when that directory is already on your `PATH`.

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | bash -s -- global 4.5.17
```

### .NET tool in `/usr/local/bin`

Installs the Amafu tool in `/usr/local/bin`, avoiding a per-user tool directory
on `PATH`. It requires .NET and administrator permission.

```bash
curl -fsSL https://raw.githubusercontent.com/Burkhardt/RAIkeep/main/scripts/InstallAmafu.sh | sudo bash -s -- system 4.5.17
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

### `list`

Read selected settings from the current user's `~/.config/RAIkeep.json5`:

```bash
amafu list temp
amafu list backup
amafu list order all
amafu list cloud all
amafu list observer all
```

Named lookups print just a value and a newline, without colors or labels:

```bash
amafu list cloud OneDrive
amafu list order OneDrive
amafu list observer Mzansi
```

Paths are absolute with `~` expanded and a trailing directory separator. Order
positions start at 1; a provider absent from `DefaultCloudOrder` returns `0`,
even if it has a configured cloud path. Observer lookups return the configured
SSH target without connecting to it.

Collection output uses the shared provider glyphs and colors. Cloud tables use
the provider glyph and name at the left margin, followed by a newline and one
tab before the double-quoted path. Observer tables use space-aligned columns. Redirected output
and `NO_COLOR` disable ANSI colors; scalar output is always undecorated.

Use selector-specific help, for example `amafu list order -h`. The top-level
`amafu list all` is rejected. Queries do not create directories, change the
configuration, probe providers, or connect to observers. A missing or malformed
configuration produces an error; use `amafu init` to create an initial file.

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
