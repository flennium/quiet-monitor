# Versioning and release channels

Quiet Monitor uses [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH-channel.sequence`.

- **Stable** releases have no suffix, for example `v1.0.0`.
- **Beta** releases are feature-complete enough to share but still need wider hardware and game testing, for example `v0.1.0-beta.1`.
- **Alpha** releases may have incomplete behavior and are intended for early contributors.

Windows package manifests require four numeric parts, so prerelease sequence numbers map to the fourth field. For example:

| Public release | Windows package version |
| --- | --- |
| `v0.1.0-beta.1` | `0.1.0.1` |
| `v0.1.0-beta.2` | `0.1.0.2` |
| `v0.1.0` | `0.1.0.0` |

The repository's root `VERSION` file is the human-readable source of truth. Executable metadata and `Package.appxmanifest` must be updated in the same commit as a release.
