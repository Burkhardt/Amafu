# Release Notes: Amafu v4.5.3

CR051 adds multiple Google Drive and corporate OneDrive accounts, named
shortcuts, and optional provider/account metadata in detection JSON. Each
account independently prefers its data subfolder. Name collisions fail with
both roots reported; identical roots reached through aliases are deduplicated.
Dropbox and iCloud discovery are unchanged.

## Personal OneDrive and automation

Exactly one personal root is selected as `OneDrive`. An explicit
`--onedrive-personal <folder>` wins; otherwise a valid configured root wins;
otherwise the longest folder name wins, with highest numeric suffix and then
ordinal path order as tie breakers. Corporate roots remain configured.
Commands never prompt interactively.

`amafu reconcile` previews additional roots and a migration of configuration to
`~/.CloudStorage` paths. `--apply` creates a uniquely named backup and applies
changes, preserving unrelated JSON5 settings and default order. Existing
unavailable entries are retained. Only an explicit personal OneDrive selection
may repoint its verified configured link. No cloud data is moved or deleted.

## Compatibility

OsLib's matching CR051 change consumes every configured cloud-root label and
classifies symbolic shortcut paths, including new descendants. Deploy updated
OsLib consumers alongside Amafu before migrating configuration. PitSeeder
selection accepts account-qualified keys present in Cloud and DefaultCloudOrder.
Python/jpit parity remains Adele's verification responsibility.

## Verification

- Amafu: **60 passed, 0 failed, 0 skipped**.
- OsLib: **147 passed, 0 failed, 0 skipped**.
- PitSeeder: **87 passed, 0 failed, 0 skipped**.
- macOS ARM64 NativeAOT publish and read-only detection smoke test passed.
- Corporate OneDrive is covered by filesystem fixtures, not a live corporate account.

These notes describe prepared source changes, not a completed package publication
or a coordinated release-chain preflight. Python parity remains unverified here.
