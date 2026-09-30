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

On macOS or Linux, this installs the zero-dependency native executable into
`/usr/local/bin`. To update, run the same block with the newer version number;
`install` replaces the existing executable. Because `/usr/local/bin` is already
on the standard shell path, no `$PATH` change and no later copy step are needed:

```bash
AMAFU_VERSION=4.4.5
case "$(uname -s)-$(uname -m)" in
  Darwin-arm64) AMAFU_RID=osx-arm64 ;;
  Darwin-x86_64) AMAFU_RID=osx-x64 ;;
  Linux-aarch64|Linux-arm64) AMAFU_RID=linux-arm64 ;;
  Linux-x86_64) AMAFU_RID=linux-x64 ;;
  *) echo "Unsupported Amafu platform: $(uname -s)-$(uname -m)" >&2; exit 1 ;;
esac
AMAFU_INSTALL_DIR="$(mktemp -d)"
AMAFU_ARCHIVE="amafu-v${AMAFU_VERSION}-${AMAFU_RID}.zip"
curl --fail --location \
  "https://github.com/Burkhardt/Amafu/releases/download/v${AMAFU_VERSION}/${AMAFU_ARCHIVE}" \
  --output "${AMAFU_INSTALL_DIR}/${AMAFU_ARCHIVE}"
curl --fail --location \
  "https://github.com/Burkhardt/Amafu/releases/download/v${AMAFU_VERSION}/${AMAFU_ARCHIVE}.sha256" \
  --output "${AMAFU_INSTALL_DIR}/${AMAFU_ARCHIVE}.sha256"
if command -v sha256sum >/dev/null 2>&1; then
  (cd "${AMAFU_INSTALL_DIR}" && sha256sum --check "${AMAFU_ARCHIVE}.sha256")
else
  (cd "${AMAFU_INSTALL_DIR}" && shasum -a 256 --check "${AMAFU_ARCHIVE}.sha256")
fi
unzip -q "${AMAFU_INSTALL_DIR}/${AMAFU_ARCHIVE}" -d "${AMAFU_INSTALL_DIR}"
sudo install -m 0755 "${AMAFU_INSTALL_DIR}/amafu" /usr/local/bin/amafu
amafu --version
```

`sudo` is used only by the OS installation command that writes
`/usr/local/bin`; never run `sudo amafu init`.

Published targets:

- macOS ARM64 and x64;
- Linux ARM64 and x64;
- Windows x64.

## Platform support in 4.4.5

Automatic cloud-provider discovery is supported and tested on **macOS only** in
this release. The Linux and Windows binaries are provided so the native command,
help, diagnostics, and starter-template fallback are available without a .NET
runtime, but provider-specific discovery on those operating systems is not yet
implemented or claimed as supported. Linux and Windows detection will be added
and tested in a later release.

### .NET global tool

Operators who already use .NET 10 can install the NuGet tool package:

```bash
dotnet tool install --global Amafu --version 4.4.5
```

or update an existing installation:

```bash
dotnet tool update --global Amafu --version 4.4.5
```

Both installations expose the same `amafu` command.

To install the NuGet tool into the shared `/usr/local/bin` tool directory:

```bash
sudo dotnet tool install Amafu \
  --tool-path /usr/local/bin \
  --version 4.4.5
```

To update that installation:

```bash
sudo dotnet tool update Amafu \
  --tool-path /usr/local/bin \
  --version 4.4.5
```

The `--tool-path /usr/local/bin` form likewise makes `amafu` immediately
available without adding the default per-user .NET tool directory to `$PATH` or
copying a launcher afterward. These commands require a compatible .NET runtime
on the target machine. Use the NativeAOT installation above when no .NET
runtime should be required.

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
- Provider auto-detection is supported and tested only on macOS in 4.4.5;
  other platforms receive the explicit starter-template fallback.
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

- Foldable class and method reference: [API.md](https://github.com/Burkhardt/Amafu/blob/main/API.md)
- Release notes: [Amafu_RELEASE_NOTES_4.4.5.md](https://github.com/Burkhardt/Amafu/blob/main/Amafu_RELEASE_NOTES_4.4.5.md)
- Governing request:
  [CR044_AIA_and_jsonpit_to_RAIkeep_Auto-Detect-Cloud-Drives-and-Init-Config.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/CR/CR044_AIA_and_jsonpit_to_RAIkeep_Auto-Detect-Cloud-Drives-and-Init-Config.md)
