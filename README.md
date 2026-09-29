# Amafu

![RAI logo](HardCastle.png)

**Amafu** (*amafu*: isiZulu, “clouds”) is the standalone RAIkeep cloud-storage
discovery and configuration bootstrap utility. Its installed command is `amafu`.

Amafu discovers supported cloud-provider roots and creates the shared
`~/.config/RAIkeep.json5` consumed by both the C# `pits` CLI and Python `jpit`.
It does not reference OsLib, JsonPit, Python, Node.js, or any third-party runtime
package, and it never loads or mutates OsLib's `Os.Config`.

## Install

### Runtime-independent native executable

Download the archive for your platform from the
[Amafu GitHub Releases](https://github.com/Burkhardt/Amafu/releases) page, verify
its adjacent SHA-256 checksum, extract `amafu` (`amafu.exe` on Windows), and put
it on your `PATH`. These NativeAOT executables do not require .NET to be
installed.

Published targets:

- macOS ARM64 and x64;
- Linux ARM64 and x64;
- Windows x64.

### .NET global tool

Operators who already use .NET 10 can install the NuGet tool package:

```bash
dotnet tool install --global Amafu --version 4.4.4
```

or update an existing installation:

```bash
dotnet tool update --global Amafu --version 4.4.4
```

Both installations expose the same `amafu` command.

## Commands

Inspect detected providers without writing:

```bash
amafu detect
amafu detect --json
```

Preview the generated JSON5 without creating any directory or file:

```bash
amafu init --dry-run
```

Create the configuration:

```bash
amafu init
```

`amafu init-config` is an alias for `amafu init`.

Amafu refuses to replace an existing configuration. Regeneration must be
explicit:

```bash
amafu init --force
```

Do not run Amafu with `sudo`. It must use the ordinary user's home directory and
must create an ordinary user-owned configuration.

## Safety boundary

- Detection is read-only and never creates cloud-provider roots.
- Dry run performs zero filesystem writes.
- An existing configuration is byte-for-byte preserved without `--force`.
- No file or directory is staged in a temp directory and moved into cloud
  storage.
- Tests use explicit fixture homes and never modify the operator's real
  `~/.config/RAIkeep.json5`.
- Amafu's internal model is `AmafuConfiguration`; it does not define an
  `Os.Config` or `OsConfig` type.

## Terminal font

> **Font note:** The `amafu` help screen uses glyph icons from Nerd Fonts. Most
> Nerd Font-patched fonts render correctly in most terminal environments. Blink
> on iPadOS showed clipping and character-width problems with some choices; the
> tested solution was Blink's
> [Jet Brains Mono Nerd Font stylesheet](https://github.com/blinksh/patched-fonts/blob/main/Jet%20Brains%20Mono%20Nerd%20Font.css).
> See the RAIkeep
> [terminal font guide](https://github.com/Burkhardt/RAIkeep/blob/main/doc/TERMINAL_FONTS.md)
> for Blink, macOS, and Ubuntu setup.

## Documentation

- Foldable class and method reference: [API.md](API.md)
- Release notes: [Amafu_RELEASE_NOTES_4.4.4.md](Amafu_RELEASE_NOTES_4.4.4.md)
- Governing request:
  [CR044_AIA_and_jsonpit_to_RAIkeep_Auto-Detect-Cloud-Drives-and-Init-Config.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/CR/CR044_AIA_and_jsonpit_to_RAIkeep_Auto-Detect-Cloud-Drives-and-Init-Config.md)
