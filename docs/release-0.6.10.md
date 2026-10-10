# SnapMark 0.6.10 — Application logo

- Original capture-frame and markup-pen logo, with navy, cyan, white and gold colors.
- Multi-resolution ICO (16, 20, 24, 32, 40, 48, 64, 128 and 256 px) embedded in the Windows executable.
- WPF windows and the system tray use the same embedded icon.
- Installer shortcuts explicitly point to the executable icon, so Start Menu/search and desktop shortcuts show the app logo.
- Editable SVG and a reproducible local icon build script are included in the source repository. No third-party artwork or icon fonts are used.

Validation: Release build and existing UI-model checks passed. Packaged executable icon extracted through the Windows icon API for inspection.
