# Release Notes: Amafu v4.5.4

Fixes `amafu init --create-links` so the generated `RAIkeep.json5` uses the
shortcuts it creates, instead of the original provider paths. For example,
`GoogleDriveRainer` now maps to `~/.CloudStorage/GoogleDriveRainer/`, and
`ICloudDrive` maps to `~/.CloudStorage/ICloudDrive/`.

```bash
amafu init --create-links --dry-run   # Preview the exact shortcut-based configuration
amafu init --create-links             # Create shortcuts and a new configuration
```

The same behavior applies to `init-config`. Dry runs create neither links nor
configuration files. Plain `amafu init` continues to use the detected physical
paths; `amafu detect --json` continues to report physical roots.

For an existing configuration, use `amafu reconcile` to preview the migration,
then `amafu reconcile --apply` to apply it with a backup. `init` still refuses
to overwrite an existing configuration unless `--force` is explicitly supplied.

## Verification

- Complete Amafu suite: 62 passed, 0 failed, 0 skipped.
- Release validation: 80 checks passed.
- Release-tool regression suite: 2 passed.

Regression coverage checks that preview and saved configuration match exactly
for both init commands, including two Google Drive accounts and iCloud Drive.
Every configured shortcut is checked against its intended target and used to
read a file from that target. Existing tests cover plain init, link conflicts,
JSON detection output, and configuration overwrite protection.

The C# packages and dependency versions are aligned to the coordinated 4.5.4
patch release. Publication is performed by the release chain.
