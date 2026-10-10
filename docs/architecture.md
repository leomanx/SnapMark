# Architecture

`SnapMark.App` is the composition root. It owns tray/hotkey lifecycle and translates
capture results into editor windows. Main-window status uses a small view model;
pointer gestures and drawing remain in WPF views. Constructor-injected interfaces
keep clipboard, export, settings, rendering and capture replaceable without a DI package.

```text
App → Capture → Core
App → Editor  → Core
App → Export  → Core
```

Capture does not reference Editor; Editor does not reference Capture or Win32.
Core contains immutable annotation records, pixel geometry, the document and editor
commands. It has no WPF dependency. Platform bitmap/result contracts live in Capture,
so the domain does not depend on BitmapSource.

## Coordinates

- Desktop bounds and cursor positions: physical screen pixels from Win32.
- Each monitor owns a frozen overlay, positioned using SetWindowPos and PerMonitorV2.
- Overlay visuals transform physical pixels to that window's layout DIPs for display only.
- A selection is intersected with desktop bounds and offset into the frozen desktop bitmap.
- Editor coordinates are image pixels. One logical canvas unit equals one image pixel.
- Zoom is a WPF LayoutTransform; GetPosition maps input back to unscaled image coordinates.
- Export ignores UI zoom and renders at 96 DPI with explicit pixel dimensions.
- Crop is a document rectangle. Export translates the image/vectors and clips at that rectangle.

## Editing

EditorSurface has explicit Idle, Drawing and Dragging states. An in-progress gesture
is a preview; mouse-up commits one command. Lost capture / Escape discards the preview.
Commands implement Execute/Undo. New commands clear redo. Deleting remembers its layer
index, and moving replaces an immutable annotation while preserving identity.

Base images are frozen BitmapSources. AnnotationRenderer is shared by preview and
export. UI selection borders and crop shading are never included in exported images.
Text/stamp selection uses renderer text metrics; resize handles are a future improvement.
ToolCatalog defines seven categories and stable tool IDs; the sidebar is generated from
this registry. Stamps are definitions/presets of one tool, not a class per label.

PixelEffects processes only source-image ROIs. Blur uses three separable box passes,
pixelate uses block averages, and solid covers are opaque black. A bounded cache keys
on immutable annotations, so movement/style changes invalidate processed results.
The shared DrawDocument pipeline renders base → blur/pixelate → solid covers → vectors.
Opaque covers cannot be revealed by adding a later overlapping blur/pixelate region.
Preview and flattened clipboard/PNG export call this same pipeline.

## Local persistence and failure handling

Settings use JSON with defaults for malformed data. PNG writes use a sibling temporary
file and rename. History has a configurable count limit and stores only originals.
Original-image history can be disabled before capture; existing saved history is retained.
Auto-copy is separately configurable since initial captures enter the clipboard before
any markup. Final Ctrl+C / PNG includes only the flattened result, not the original.
Settings, history, capture and clipboard errors are reported without losing the editor
image. Local error logs record operation and exception type, not screenshot data.
No external packages or network services are used by application code.

## Next extension points

### Implemented in 0.3

Recording now has its own `SnapMark.Recording` project, with bounded progress/error
streams, readiness detection, serialized Stop, temporary MP4 and final rename.
FFmpeg is an external local executable, not a NuGet dependency or bundled download.
The editor model stores path points, fill/gradient/opacity, rotation and endpoint markers.
Image assets are embedded PNGs; renderer composites base + image objects, then effects,
opaque covers and vectors. Canvas expansion is undoable. `.snapmark` is a versioned ZIP
with original PNG, document JSON and referenced assets, validated on load and written
atomically. No autosave or cloud integration. See [release guide](release-0.3.md).

The following extension notes predate the implemented recording/document features.

Add separate OCR/Recording/ArchTools projects when their first real feature is implemented.
Introduce an independent plugin contracts assembly before loading plugins; do not expose
Bootstrapper or EditorWindow as the plugin API. Persistent document serialization must
version annotations and preserve original image/crop metadata. No placeholder services
claim unsupported capabilities in this release.
