# Amafu API and Internal Design Reference

Amafu is distributed as a CLI rather than a reusable library. Its implementation
types remain internal so consumers depend on the stable command and JSON output
contracts instead of linking to bootstrap internals.

<details>
<summary><code>AmafuApplication</code></summary>

Dispatches global flags and the `detect`, `init`, `init-config`, and `reconcile` verbs.
Returns deterministic process exit codes and resolves no global configuration.

- `Run(string[] args, AmafuRuntime runtime)` — executes one invocation against
  an explicit runtime context.

</details>

<details>
<summary><code>AmafuRuntime</code></summary>

Carries the explicit home directory, platform, shared Google Drive candidate,
elevation state, terminal width, and output streams. Production creates it from
the current process; tests construct isolated instances without changing global
environment state.

</details>

<details>
<summary><code>CloudStorageDetector</code></summary>

Performs read-only, deterministic cloud-root discovery. It returns confirmed
provider roots and the paths checked during discovery. It never creates or
hydrates a provider directory. Google and OneDrive discovery return multiple
account-aware records. Personal OneDrive uses explicit selection, then an
existing configured root, then longest name / highest suffix. Corporate roots
remain independent. Name collisions fail before filesystem writes.

- `Detect(AmafuRuntime runtime)` — discovers providers for the explicit runtime.

</details>

<details>
<summary><code>CloudStorageLinks</code></summary>

Creates `~/.CloudStorage` and provider symlinks only when `--create-links` is
requested on `detect` or `init`. Validates all destinations before creation,
reuses matching links, and refuses existing conflicting files, directories, or
links. Never creates provider roots. `--dry-run` bypasses all writes.

</details>

<details>
<summary><code>AmafuConfiguration</code></summary>

The internal strongly typed representation rendered to `RAIkeep.json5`. It is
intentionally unrelated to OsLib's immutable `Os.Config` runtime snapshot.

</details>

<details>
<summary><code>AmafuConfigurationRenderer</code></summary>

Produces deterministic JSON5 configuration and strict JSON detection output.
It shortens paths below the supplied home to `~/` and performs explicit JSON
string escaping without reflection-based serialization.

- `Render(AmafuConfiguration configuration)` — renders the shared JSON5 file.
- `RenderDetectionJson(CloudDetectionResult result, string homeDirectory)` —
  renders the machine-readable `detect --json` contract.
- `ToPortablePath(string path, string homeDirectory)` — creates a normalized,
  trailing-slash display path.

</details>

<details>
<summary><code>AmafuConfigurationWriter</code></summary>

Creates or explicitly replaces only the resolved configuration file. It applies
Unix mode `0644`, refuses implicit overwrite, and never stages through a temp or
cloud directory.

- `Write(string destination, string payload, bool force)` — writes a completely
  rendered payload under the explicit overwrite policy.

</details>

<details>
<summary><code>CliHelp</code></summary>

Renders the RAIkeep Nerd Font banner, aligned option table, command-specific
help, and narrow-terminal wrapping without an external CLI framework.

</details>

<details>
<summary><code>ConfigurationDocument / CloudConfigurationReconciler</code></summary>

Read existing JSON5 without loading OsLib. Reconcile the Cloud and
DefaultCloudOrder values while preserving unrelated source text. Reconciliation
previews by default; explicit apply backs up the original, creates shortcuts,
and replaces the configuration through a sibling file. Unknown root entries are
preserved. Explicit personal OneDrive selection can change its verified
configured symbolic link without changing either root's data.

</details>
