# SnapMark 0.6.12 — Visible input labels and examples

Release preparation update (same 0.6.12): source-available LICENSE owned by
Teerapat Chumrassomboon permits company/professional use but restricts software
resale. Added quick start, portable update, plugin, contribution and security
guides, current roadmap audit and package ZIP checksum. Settings includes
Browse/Auto for a persisted FFmpeg executable preference. The portable package
includes a compiled example exporter DLL. Validation: 425 assertions passed with
UI model, desktop/video and actual external exporter checks; no new microphone
listening check. See [release audit](release-readiness.md).

Path fields now explain what to enter using visible captions and examples, without requiring a mouse hover. Accessibility names and help text carry the same guidance.

- History distinguishes its search field from the read-only storage folder and explains how to change that folder in Settings.
- Settings explains folder paths, filename patterns and shortcut syntax.
- Team folders distinguishes local synced folders from optional provider web links.
- Install identifies the extracted package folder containing SnapMark.exe and release-manifest.json.
- Video tools identifies a source video file and explains start/end times in seconds.
- Profiles explains the image export folder and the empty-field fallback.
- OCR and command palette search fields now have visible names and examples.

The audit also checked photo tools, PDF import, compare and editor inputs. Their existing labels, units or file-selection dialogs identify the expected input; none adds another editable path field.

Additional editor update (version remains 0.6.12):

- Compact category ribbon shows all seven tabs without stretching buttons across the window.
- Drag category or subtool buttons onto another button to reorder them. Right-click Move earlier/later provides an alternative. Orders are saved locally in data/editor-layout.json.
- Drag the vertical accent divider to widen the tool panel; drag the horizontal divider below the tool list to change its height. Both sizes are remembered.
- Right-click a History thumbnail and choose Rename to give it a searchable name (including Thai text). Empty names restore the date label. Image filenames, timestamps, pixels and OCR cache remain unchanged; the name lives in its metadata sidecar.
- History search includes these names without requiring OCR indexing.

Validation: Release build passed without warnings; the suite passed 332 assertions including UI-model checks for saved layout, Unicode history names and name search. Native window inspection verified category dragging, width/height resizing and the History rename menu/dialog.

Measurement label update (same 0.6.12):

- Dimension/Distance labels use their actual text width to stay centered over the endpoint midpoint as calibration changes the displayed value. Length/Area/Angle labels center over their measured bounds.
- Measurement backgrounds respect the existing enable switch, color, opacity and padding; there is no unconditional white box.
- Select (V) a measurement to change these properties, italic/underline, decimal precision (0–6), unit visibility and prefix/suffix. Manual Dimension text remains editable; clear it to use the calibrated automatic distance.
- Formatting is retained in editable projects, and the shared renderer applies it in both editor and exports.

Validation: Release build passed; 338 assertions passed, including centered background bounds before/after calibration, background-off rendering, numeric formatting, manual labels and editable project round-trip.

Symbols Tools update (same 0.6.12):

- All 14 built-in symbol variants now appear as direct buttons in Tools: North, Section, Elevation, Detail, Grid, Level, Centerline, Axis, Break, Slope, Check, Cross, Warning and Issue.
- Buttons preview the same vector geometry as placed symbols, highlight the current variant and support saved drag reordering.
- The creation dropdown is removed; label/font/sequence and imported SVG controls remain below Tools. Select properties continues to edit existing symbols.
- Validation: 354 assertions passed, including every direct variant button and selection retained across category switching.

Selected-object transform and alignment update (same 0.6.12):

- Select properties includes horizontal/vertical mirror, absolute 0/45/90/180/270-degree guides, ±45-degree steps and custom degrees. Transform actions are undoable and stored in editable projects.
- Shapes, labels and inserted image layers use the same centered mirror/rotation transform in editor and export. Selection, marquee and vertex coordinates follow it.
- Text alignment uses a real text frame instead of a frame exactly as wide as the glyphs. Select properties exposes text-frame width (0 = auto); selecting an alignment creates a wider frame when required. Text, Note, Stamp, Symbol, Step, Callout and Dimension render single-line left/center/right positions consistently.
- Validation: Release compilation and 365 assertions passed, including rendered alignment positions for seven label kinds, transform inversion, rotation-field refresh, independent Undo and project round-trip.

Shape handles update (same 0.6.12):

- Selected rectangular/bounded shapes show four corner handles. Dragging any corner resizes from that corner while keeping its opposite fixed, including rotated/mirrored objects. Circles retain equal width/height.
- Triangle shows three vertices; polygon/path shapes expose their stored vertices directly without first enabling Edit vertices. Dense freehand paths retain bounding-box handles unless vertex mode is enabled.
- Lines, arrows, dimensions and callouts expose both endpoints. Handle size and hit radius remain usable at different zoom levels.
- Vertex/corner edits remain undoable. Moving a triangle vertex converts its geometry to an editable polygon while preserving untouched visible vertices.
- Validation: Release build and 398 assertions passed, including all four corners under 0/45/90-degree rotation with mirroring, fixed opposite corners, triangle/endpoints and Undo.

Color picking and selection marquee update (same 0.6.12):

- Each editor color picker includes a vector eyedropper beside its wheel. It targets that exact swatch/HEX field, including selected-object stroke, fill, label and text-background colors. Primary/background/gradient creation colors have their own buttons.
- Sampling is transient: it preserves the active tool/category and selection, shows the target in the status bar, and supports Esc cancellation. The generic Pick color button is removed from Tools and the command palette.
- Select (V) can drag from blank canvas to select objects. Left-to-right selects fully enclosed objects; right-to-left selects crossing objects. Ctrl or Shift adds to the current selection. Delete removes the selected batch, and one Undo restores it.
- Validation: Release compilation and 407 assertions passed, including target-specific color updates, retained active tool, cancellation, single/multi marquee selection, additive selection and batch Delete/Undo.
