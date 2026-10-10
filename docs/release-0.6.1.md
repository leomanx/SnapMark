# SnapMark 0.6.1 · Symbol labels and selected object properties

Symbols supports Custom, Alphabet (A…Z, AA…), Numeric and Prefix modes.
Set the next/start number, prefix (for example `RFI-` or `D-`), numeric/alphabet
suffix and symbol font size before dragging. Successfully placing a sequenced
symbol advances the counter. Editing an existing label/font does not renumber it.

Press **V**, then click an object from any category. **Object Properties** appears
on the right with supported fields. Color/picker/checkbox/dropdown changes apply
immediately; numeric fields apply on Enter or leaving the field. Label editing
uses **Apply label** to allow multiline typing. Ctrl+Z restores property edits.

- Drawing: stroke color, weight/pattern, border opacity, independent fill color,
  fill opacity/type/gradient, line endpoints, closed freeform/polyline loops and rotation.
- Symbols/text/callouts/stamps: font size/family, label color, editable label,
  border/background where supported, and symbol type. Text also has background
  enable/color/opacity/padding. Symbol font size is near the top of Properties.
- Image layers: opacity and rotation. Blur/pixelate: effect radius/block size.
  Solid cover remains opaque black. Line-only symbols do not show fill controls.
- Multi-selection shows common supported properties, displaying the last-selected
  object's values. Changing one field changes only that field, leaving other values
  intact. One undo restores the whole batch.

Independent fill/stroke/label properties persist in editable projects and personal
styles. Old projects retain legacy defaults. Empty saved-style/SVG lists are disabled
with explanatory text, instead of presenting unexplained empty dropdowns.

Validation: 241 assertions including existing capture/video/English OCR workflows,
independent alpha rendering, project/preset round trips, mixed-selection edits/undo,
alphabet/prefix sequences and UI-model selected-symbol font editing. Live UI inspection
confirmed creating a symbol and using V/click to open Object Properties.
Logs: `artifacts/0.6.1-validation.log`, `artifacts/0.6.1-build.log`.
Audio work and real cloud-account setup remain as described in 0.6.0.
