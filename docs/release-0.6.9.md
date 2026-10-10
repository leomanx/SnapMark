# SnapMark 0.6.9 — Photo workspace and print layout preview

- Resizable three-pane Photo Tools workspace: file list, settings and a large preview. Drag the vertical dividers to change space allocation.
- Photo preview supports Fit page, 100% pixels, zoom buttons, Ctrl+wheel and scrolling. Tone/resize changes update automatically after a short debounce.
- Auto preview shows one photo in detail or multiple selected photos on a print page. Print layout follows the same paper, orientation, grid, margins and captions used by Print preview. Previous/Next paginates the selection. Preview modes can be chosen explicitly.
- Paper presets: ISO A0–A6, ISO B0–B6, Letter, Legal and Tabloid. Portrait/landscape is independent. Dimensions verified against [Adobe's paper-size reference](https://www.adobe.com/uk/creativecloud/design/discover/guide-paper-sizes.html).
- Physical margin/inset units: millimeters, inches or feet-inches. Examples: `1/8"`, `1' 2.5"`, `1'-2 1/2"`. Bare values in FootInch mode are inches. Switching units preserves the physical lengths.
- Add folder imports supported image files directly inside the chosen folder, filters extensions without case sensitivity, and avoids duplicate paths. Non-image files are skipped; GIF export retains its 500-frame limit.
- Filename captions: Hidden, Below, OverlayBottom and OverlayTop. Inset applies from the actual image edge, including images with white space inside a layout cell. Font size uses points. Overlay captions have a translucent backing for readability.
- Large selections preview one page at a time; superseded preview work is cancelled. Print preview uses source-resolution images.

Validation: Release build passed without warnings/errors. 336 assertions passed, covering paper dimensions, fractional inch/foot inputs, physical unit conversion, folder filtering, caption visibility/placement, 100% pixel preview, image conversions, actual GIF output and existing editor/capture/recording behavior. Native UI checked with a folder containing uppercase/lowercase PNG files and a TXT file; single-photo zoom and multiple-photo layout were inspected. Physical printing remains untested.
