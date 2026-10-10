# Validation

## Release preparation — 0.6.12, 2026-10-11

- 425 assertions passed with `--ui-model --capture --recording --plugin`.
- Real desktop capture/MP4 decode, compiled external exporter load and valid PNG
  decode, persisted FFmpeg preference/precedence and missing-path handling passed.
- Portable Release publish passed with zero warnings/errors. See the current
  [release audit](release-readiness.md) for pending hardware/clean-machine checks.
- No new real microphone/loopback listening check was performed in this update.

Older results below describe their respective versions.

## Storage folders — 0.4.1

- Separate persisted image/PDF, video/trim/GIF, project, history and recovery folders.
- Standard runner: 143 assertions; full desktop/video runner: 151 assertions.
- New checks cover persistence, portable default/reset semantics, rejecting relative paths,
  preserving unrelated PNG files under history retention, and keeping previous captures
  when switching history folders. Recovery changes apply after restart; output/history apply immediately.

## Local workflow jobs — 0.4.0

- Release build: zero warnings/errors. Standard runner: 138 assertions; desktop/video suite: 146.
- New coverage: recovery isolation and atomic replacement, background recovery from decoded PNG,
  hotkeys/naming, preset persistence, rotated vertex edit/undo, SVG library, pixel diff,
  PNG/JPEG/BMP/TIFF round trips and PDF round-trip via Windows PDF engine.
- Real local English OCR recognizes a rendered text fixture. Installed OCR language on this machine: en-US.
- Scrolling reconstructs exact pixels from overlapping frames, ignores duplicates and supports undo.
- Real FFmpeg desktop MP4 capture starts/stops and decodes. Synthetic audio/video trim keeps audio,
  has the requested duration, preserves source bytes; animated GIF decodes with multiple frames.
- Live Windows UI: drawn markup autosaved; process interrupted to simulate crash; recovery list
  reopened the original image with its editable annotation. OCR recognized the demo drawing labels.
  Before/after overlay and diff showed the drawn arrow, reporting 1502 changed pixels / 0.32%.
- Local installer verified in a workspace destination, including idempotent rerun and exclusion of
  portable.flag/data. Corrupted package hash was rejected before creating a destination.
- Live Diagnostics: two monitors, 2560×1440 and 1920×1080, negative desktop origin -1920,
  current window 96 DPI. A 900×520 render with 300 rectangles measured 18.4 ms in one run;
  this is a fixture measurement, not a guarantee for all documents/hardware.
- Thai OCR, real microphone timing, native loopback, mixed-DPI/monitor disconnect/HDR matrix remain
  unverified or outside current engine scope; see [0.4 scope](release-0.4.md).

## Color / opacity / grouping — 0.3.3, 2026-10-05

- Release build: zero warnings/errors; standard runner passes 107 assertions.
- Five new checks: HSV primary hues, hue wrapping, white/black, shorthand HEX and
  rejection of malformed inputs, RGB/HSV round trip.
- Live synthetic-drawing UI: entered 37% and verified slider 0.37; selected custom
  blue #3198EF from the wheel, applied it, drew a rectangle with that color/opacity.
  Changed tool opacity to 100%, selected the rectangle, verified options reload 37%.
- Inspected grouped toolbar, option cards and wheel dialog. Primary and gradient
  share the same picker. Recent colors are per session; Pantone is not included.
- Final source adds a nearby Apply style button and reloads properties after undo.

## Marquee Eraser / 0.3.2 — 2026-10-05

- Release build: zero warnings/errors; 102 assertions in the standard runner.
- Eight new assertions cover directional window/crossing selection, actual diagonal
  line intersection, rotated bounds, batch delete, one-step undo preserving stacking
  around retained objects, redo and empty-region no-op.
- Existing multi-selection deletion now executes in descending stacking order so
  composite undo restores deleted objects in ascending order.
- This patch was verified by build and automated geometry/command tests; no live UI
  restart was performed because the running editor has unsaved work.

## All seven categories / 0.3.0

- Final Release build: zero warnings/errors; 99 assertions with `--capture --recording`.
- Covers every active category, expanded shape/symbol rendering, dash/gradient/opacity,
  alphabetic sequences through AA/AZ/XX, move/resize, command undo, scale/length/area/angle,
  embedded images under redaction, canvas expansion and exact-pixel `.snapmark` round trip.
- Real FFmpeg recording smoke: odd-size desktop region, startup readiness, stop/finalize,
  then full MP4 decode with no errors. Temporary test media removed afterward.
- Live synthetic-drawing UI: constrained circle, three-click polygon + Enter, Step starts
  at 12 and advances to 13, history picker inserts a selectable image with resize handle.
- History/control window theme corrected after UI inspection; contextual options show
  Step controls beside the Step tool. Native Save As automation remains limited; export
  and project storage were validated programmatically, not via automated native dialogs.
- Mixed-DPI hardware, long recordings, audio and crash recovery have not been validated.
- Delivered self-contained executable reports 0.3.0 and launches Ready with hotkeys.
  Video setup and history were inspected again in this portable build: text visible,
  Region/FPS/cursor options present, history preview/actions readable in the dark theme.

Older results below describe the features and limits of their respective versions.

## Design MVP P1-A / 0.2.0

- Final Release build: zero warnings/errors; 39 assertions passed with `--capture`.
- New coverage: seven-category registry, 29 stamp labels, pixelate block averages,
  blur detail reduction, original-image preservation, clipped/one-pixel ROIs, cache
  invalidation on movement/strength, solid cover overlapping later pixelate, cropped
  redacted PNG round trip, effect/stamp undo/redo and history opt-out settings.
- Actual WPF EditorSurface preview pixels compared equal to flattened renderer output
  for a stamp and solid cover. Clipboard and PNG use this renderer.
- Live UI on a synthetic design drawing: B opened Highlight & Redact; region drag
  blurred example contact text; S opened Review; RFI stamp placed correctly;
  Ctrl+Z removed the stamp while retaining blur and Ctrl+Y restored it.
- Blur uses three separable box passes approximating Gaussian blur, not a native
  GPU filter. Large ROIs still need performance profiling on target hardware.
- Dimension/Symbol categories are visible as Coming next; no placeholder tools claim
  these capabilities are implemented. Native Save As automation limitation remains.
- Self-contained Windows x64 portable package updated in `artifacts/SnapMark-portable`.

## Foundation / 0.1.0

- Release solution build: zero warnings, zero errors.
- Self-contained Windows x64 single-file publish succeeded; published executable launched
  and its main window displayed Ready with global hotkeys registered on this machine.
- Dependency-free test runner: 17 assertions passed, including real desktop capture.
- Geometry: reverse drag, negative origins, monitor intersection.
- Editor: undo/redo, redo invalidation, move/delete ordering, non-destructive crop.
- Rendering: cropped export dimensions, source DPI independence, pixel-level annotation output.
- Storage: PNG round trip, settings round trip/malformed JSON recovery, history retention.
- Live Windows UI: home screen and theme inspected; two monitor overlays appeared;
  dragged an 880×230 region and verified it opened in the editor.
- Live editor: Arrow, Rectangle and Thai/English Text rendered; Ctrl+Z/Ctrl+Y worked;
  Ctrl+C reported success. PNG save dialog opened. The UI automation helper could not
  reliably target the native save dialog filename field, so final save through that
  dialog was not automated. PNG service output is covered by the round-trip test.

Not yet verified: mixed scaling across monitors, dragging across monitor boundaries,
hotplug, protected content, HDR, third-party paste compatibility, stress/performance
targets, and all hotkeys under other screenshot applications.
