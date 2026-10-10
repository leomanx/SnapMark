# Security and privacy

History keeps original captures. Editable `.snapmark` projects and recovery copies
can retain original pixels and reversible effects. Disable Keep history/auto-copy
before sensitive capture, and review existing files and clipboard content separately.
For sharing redacted content, use opaque Solid cover and export a flattened image;
blur/pixelate are not guarantees against recovery. Inspect the exported result.

Plugins run arbitrary trusted .NET code in the SnapMark process with the user's
permissions. The narrow exporter API is not a sandbox. It cannot prevent a plugin
from reading files or using the network. Load only reviewed code from a trusted
source; restart to unload. FFmpeg is also an external executable you choose.

Team folders are ordinary local folders. Their sync/sharing access is managed by
Google/Microsoft and the user. SnapMark does not establish recipient permissions.
Do not publish `data`, recovery files, private captures or account secrets.

Release SHA-256 files help detect damaged or changed downloads when compared
against a trusted checksum. They are not executable signatures. SnapMark currently
has no telemetry, automatic update service or formal security response SLA.

A private security-report contact must be configured before public publication.
Until then, communicate privately with the maintainer through an established
channel. Do not disclose sensitive samples or an exploitable issue publicly.
