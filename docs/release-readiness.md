# Release preparation audit — 0.6.12, 2026-10-11

The Design MVP and main local workflows are implemented. The original 120-topic
blueprint is not complete: it includes future architecture/ecosystem scope.
This preparation keeps version 0.6.12 and targets Portable Beta distribution.

| Latest group | Prepared | Remaining before claiming completion |
|---|---|---|
| 1. License / ownership | Custom source-available LICENSE; Teerapat Chumrassomboon; use in companies/paid work allowed, software resale restricted; separate third-party notices | No MIT or OSI-open-source claim |
| 2. Documentation | Current README/roadmap, quick start, plugin guide, known issues, portable update steps; public portfolio repo leomanx/SnapMark | Keep docs current as features change |
| 3. Release QA | Local automated geometry/editor/storage/desktop/video/plugin checks; recorded evidence below | Clean Windows machine; mixed DPI/HDR; Both-mode listening; long recordings; Thai OCR; browser extension runtime |
| 4. FFmpeg readiness | Persisted Settings Browse/Auto file preference and status; diagnostics resolves preference; still-image tools independent | FFmpeg not bundled; any future bundle needs exact-build licensing review |
| 5. Maintenance / package | CONTRIBUTING, SECURITY, public GitHub Issues, manifest-based portable ZIP excluding data, ZIP SHA-256 sidecar | Dedicated private security contact not configured; executable signing optional and currently absent |

## Local checks versus external acceptance

Build/test results for this update are recorded in
`artifacts/0.6.12-release-preparation-validation.log`: **425 assertions passed**
with `--ui-model --capture --recording --plugin` (no new live microphone test).
The compiled external exporter wrote a valid 3×2 PNG, and real MP4 capture
finalized and decoded successfully. Portable publishing succeeded with zero
build warnings/errors. A clean-machine test cannot
be claimed from running on the development machine. Use this matrix for actual QA:

| Test | Acceptance | Current state |
|---|---|---|
| Fresh Windows x64, no .NET SDK | Extract, launch without admin, capture/edit/copy/save, exit | Pending external machine |
| FFmpeg absent / selected / moved | Image editing works; clear unavailable status; Browse restores recording | Resolver/persistence automated; recording local smoke |
| Two monitors with different DPI | Region bounds/MP4 pixel size correct across screens | Broader hardware pending |
| System + JLAB simultaneously | Listen to both tracks in finalized MP4; start/stop, 30+ minute drift | Final listening/session check pending |
| Recovery / update | Save/reopen, recovery after interruption; update preserves custom paths/data | Local automated coverage; external acceptance pending |
| Thai OCR / scrolling / extension | Known real text/pages, expected output and cancellation | Wider real-world QA pending |
| Plugins | Load actual DLL, export PNG, decode and verify pixels/dimensions | Automated external assembly smoke |

The license reflects the owner's requested no-resale policy, rather than a standard
MIT license. The executable/ZIP must be described as **source-available**.
Portfolio repository: https://github.com/leomanx/SnapMark. Hardware checks remain
open for the Portable Beta; no account integration, registration or signing was performed.
