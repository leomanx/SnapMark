# Portable release and updates

Keep version 0.6.12 for this preparation update. No installer is required.

## Update safely

1. Save editable work and stop/finalize any recording. Exit through the tray.
2. Back up the existing `data` folder and any custom folders configured in
   Settings. Include history, recovery, settings, presets and editor layout.
3. Extract the new ZIP into a new writable folder. Keep `portable.flag`.
4. Copy your backed-up `data` into that folder, then launch its SnapMark.exe.
   Custom absolute storage and FFmpeg paths continue pointing to their existing
   locations; review them if moving to another computer.
5. Point your shortcut at the new executable. Retain the previous folder until
   you confirm settings, history and saved projects open correctly.

Do not overwrite a running executable. Portable ZIPs exclude `data`; the local
manifest also excludes it. Export folders outside `data` require separate backup.
Same-version updates can differ: compare the ZIP SHA-256, not just version text.
Checksums establish file integrity, not publisher identity; this build is unsigned.

## FFmpeg

FFmpeg is not bundled. Use Settings → Video encoder → Browse to select its
executable. Keep any DLLs required by that FFmpeg build beside it. A found file
does not establish codec/device compatibility; run a short recording and decode
the result. SnapMark currently needs Windows desktop capture and H.264/AAC support.
Clear the configured path with Auto to resume automatic search. A missing explicit
path reports unavailable rather than silently selecting a different executable.

FFmpeg has separate license terms dependent on build configuration:
[official FFmpeg licensing](https://www.ffmpeg.org/legal.html). If distributing
it in a future package, first record the exact build, configuration, notices and
corresponding source obligations. SnapMark's no-resale terms do not apply to FFmpeg.

## Maintainer package commands

```powershell
./scripts/publish-portable.ps1 -OutputFolder ./artifacts/SnapMark-0.6.12-release-portable
./scripts/pack-release.ps1 -SourceFolder ./artifacts/SnapMark-0.6.12-release-portable
```

The packer verifies manifest hashes and includes only manifested files plus
`portable.flag` and the manifest. It writes a `.zip.sha256` sidecar. Review the
archive before upload and complete the [release audit](release-readiness.md).
