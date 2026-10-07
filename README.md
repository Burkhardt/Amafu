# Amafu

## 4.5.5

Coordinated 4.5.5 release; public behavior is aligned with the synchronized platform.

Release notes: [Amafu_RELEASE_NOTES_4.5.5.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.5.md).

## 4.5.4

`amafu init --create-links` now writes `~/.CloudStorage/<provider>/` paths
into the generated configuration. `--dry-run` previews those same paths without
creating files or links. For an existing configuration, preview with
`amafu reconcile`, then migrate with `amafu reconcile --apply` (with a backup).

Release notes: [Amafu_RELEASE_NOTES_4.5.4.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.4.md).

## 4.5.3

CR051 adds multiple Google Drive and corporate OneDrive accounts, deterministic
personal OneDrive selection, account metadata, and noninteractive configuration
reconciliation. OsLib must be updated alongside Amafu to recognize named roots.

Release notes: [Amafu_RELEASE_NOTES_4.5.3.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.3.md).

## 4.5.2

Adds optional `--create-links` cloud shortcuts under `~/.CloudStorage` for `detect` and `init`, with read-only previews and preservation of existing paths.

Release notes: [Amafu_RELEASE_NOTES_4.5.2.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.2.md).

## 4.5.0

Coordinated 4.5.0 release; cloud configuration behavior is unchanged.

Release notes: [Amafu_RELEASE_NOTES_4.5.0.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.0.md).

## 4.4.8

Coordinated 4.4.8 release; cloud configuration behavior is unchanged.

Release notes: [Amafu_RELEASE_NOTES_4.4.8.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.4.8.md).

## 4.4.6

Participates in the synchronized 4.4.6 release; reports `amafu v4.4.6`. Cloud configuration behavior is unchanged.

Release notes: [Amafu_RELEASE_NOTES_4.4.6.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.4.6.md).


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
AMAFU_VERSION=4.5.5
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

## Platform support in 4.5.5

Automatic cloud-provider discovery is supported and tested on **macOS only** in
this release. The Linux and Windows binaries are provided so the native command,
help, diagnostics, and starter-template fallback are available without a .NET
runtime, but provider-specific discovery on those operating systems is not yet
implemented or claimed as supported. Linux and Windows detection will be added
and tested in a later release.

### .NET global tool

Operators who already use .NET 10 can install the NuGet tool package:

```bash
dotnet tool install --global Amafu --version 4.5.5
```

or update an existing installation:

```bash
dotnet tool update --global Amafu --version 4.5.5
```

Both installations expose the same `amafu` command.

To install the NuGet tool into the shared `/usr/local/bin` tool directory:

```bash
sudo dotnet tool install Amafu \
  --tool-path /usr/local/bin \
  --version 4.5.5
```

To update that installation:

```bash
sudo dotnet tool update Amafu \
  --tool-path /usr/local/bin \
  --version 4.5.5
```

The `--tool-path /usr/local/bin` form likewise makes `amafu` immediately
available without adding the default per-user .NET tool directory to `$PATH` or
copying a launcher afterward. These commands require a compatible .NET runtime
on the target machine. Use the NativeAOT installation above when no .NET
runtime should be required.

## Commands

### Account discovery and reconciliation (4.5.3)

Amafu discovers every Google Drive account and each corporate OneDrive root.
For example, `GoogleDrive-rainer.burkhardt@gmail.com` becomes
`GoogleDriveRainer`, and `GoogleDrive-yebo@umshadisi.com` becomes
`GoogleDriveYebo`. Each account independently uses its existing `GDriveData`
subfolder, or `My Drive` when that subfolder is absent. Corporate roots such as
`OneDrive-AfricaStage` and `OneDrive - Contoso` become `OneDriveAfricaStage` and
`OneDriveContoso`. Dropbox and iCloud discovery retain their previous behavior.

Exactly one personal OneDrive root is selected under the `OneDrive` key:

1. An explicit `--onedrive-personal` folder name or path wins.
2. Otherwise, keep an existing valid configured personal root.
3. Otherwise, choose the longest folder name among `OneDrive` and
   `OneDrive-Personal*`; ties use the highest numeric suffix, then ordinal path
   order. Thus `OneDrive-Personal(9)` wins over `OneDrive-Personal(2)` when there
   is no configured preference. This is a naming heuristic, not a test of which
   account is newest or currently syncing.

All detected corporate accounts are retained. No interactive prompt is used.
The selected personal root is visible in output; other checked paths are also
listed. Override the selection explicitly when needed:

```bash
amafu detect --onedrive-personal 'OneDrive-Personal(2)' --json
```

Detection JSON keeps `name` and `path`, and adds `provider` and `account` when
known. Names are stable when accounts are added. Case-insensitive account-name
collisions report both roots and fail before writes. Google short names use the
part before the first `.` or `@`, strip non-ASCII-alphanumerics, then capitalize
its initial letter. Empty names fail. Duplicate aliases of one root are folded.

To discover additional accounts and propose switching an existing configuration
to the `~/.CloudStorage` shortcuts:

```bash
amafu reconcile                       # preview only, no writes
amafu reconcile --apply               # back up, create shortcuts, update config

# Select a different personal OneDrive noninteractively:
amafu reconcile --onedrive-personal 'OneDrive-Personal(2)'
amafu reconcile --onedrive-personal 'OneDrive-Personal(2)' --apply
```

Reconciliation retains existing settings and default order, adding newly
found roots. A second label for an already configured root is not added to the
scan order again. Unknown or unavailable configured roots are retained. The
`Cloud` and `DefaultCloudOrder` sections are reformatted; unrelated JSON5 text
(including its comments and custom settings) stays intact. Unsupported or
ambiguous input fails without rewriting it. The original configuration is saved
as `RAIkeep.json5.before-reconcile-<timestamp>-<unique suffix>` before applying.
The final configuration replacement uses a sibling temporary file and preserves
its Unix permissions.

Ordinary link creation never replaces existing paths. Reconciliation may
repoint the `OneDrive` symbolic link only for an explicit personal selection,
only when that link matches the previously configured root, and only with
`--apply`. Other conflicts fail before writes. No provider data is moved or
removed. If a later write fails, the backup remains and successfully created
new shortcuts may remain; rerun the preview before retrying.

Use the corresponding updated OsLib consumers before switching configuration
keys or paths: older OsLib releases recognized only four literal cloud keys.
Restart long-running OsLib consumers after applying a configuration change; their
configuration snapshot is captured at startup. Amafu owns discovery and configuration
generation; Os.Config consumes all path
entries in `Cloud`, while `DefaultCloudOrder` governs selection and preference.

### Cloud shortcuts (4.5.2)

Opt in to short cloud paths with `--create-links`:

```bash
amafu detect --create-links --dry-run  # preview without writing
amafu detect --create-links           # create missing shortcuts
```

For example, when these provider roots are detected:

```text
~/.CloudStorage/GoogleDrive -> ~/Library/CloudStorage/GoogleDrive-user@example.com/My Drive/
~/.CloudStorage/ICloudDrive -> ~/Library/Mobile Documents/com~apple~CloudDocs/
```

Amafu creates `~/.CloudStorage` when needed and creates symbolic links for
detected providers, including OneDrive and Dropbox. The cloud files stay at
their original locations. Links use the root selected by detection, including
an existing provider data subfolder such as `OneDriveData`, when applicable.
Missing provider roots are not created: a new local directory alone would not
configure a cloud client.

Repeated runs reuse matching links. An existing file, directory, or link to a
different target causes an error; `--force` does not replace shortcuts. All link
destinations are checked before creation. If a later filesystem operation
fails, links already created are retained and the command can be retried.

Use `amafu init --create-links` to create shortcuts along with a new
configuration that uses `~/.CloudStorage/<provider>/` paths. Preview that exact
configuration with `amafu init --create-links --dry-run`; no links or files are
created during a dry run. Plain `amafu init` uses the detected real roots.
`detect --json` continues to report real provider paths, and
`detect --create-links --json` creates shortcuts while keeping stdout valid JSON.
To migrate an existing configuration while preserving its other settings, use
`amafu reconcile` to preview and `amafu reconcile --apply` to apply with a backup.

#### Finder Favorites on macOS

After creating the shortcuts, open their directory in your graphical login
session:

```bash
open "$HOME/.CloudStorage"
```

Drag the desired provider folders into Finder's Favorites sidebar. This adds
convenient access without moving cloud data; see
[Apple's Finder sidebar guide](https://support.apple.com/en-gb/guide/mac-help/mchl83c9e8b8/mac).
Amafu does not currently change Finder Favorites automatically.

### Detection and configuration

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

- Detection is read-only unless `--create-links` is supplied, and never creates
  cloud-provider roots. Shortcut creation does not overwrite existing paths.
- Provider auto-detection is supported and tested only on macOS in 4.5.5;
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
- Latest release notes: [Amafu_RELEASE_NOTES_4.5.5.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/Amafu_RELEASE_NOTES_4.5.5.md)
- Governing request:
  [CR044_AIA_and_jsonpit_to_RAIkeep_Auto-Detect-Cloud-Drives-and-Init-Config.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/CR/CR044_AIA_and_jsonpit_to_RAIkeep_Auto-Detect-Cloud-Drives-and-Init-Config.md)
