# SnapMark 0.6.8 — Calibrated perimeter and Photo Tools

## Measure

- Length: click consecutive points, then click the start to close a loop. Enter finishes an open route; the Close loop option also remains available.
- P is the perimeter, including the return segment exactly once. Area displays both A (area) and P (perimeter).
- Scale: draw an edge whose real length is known, then enter that length and unit. Existing Length/Area annotations update automatically; length uses scale and area uses scale squared.
- Select an existing polygon, polyline or freeform path, then use Selection → Length / Perimeter or Selection → Area + Perimeter. Conversion and calibration support undo.

## Photo Tools

Open **Photo Tools** from **Files & History** on the home screen.

- Add multiple photos or drag files into the list. Ctrl/Shift selects a subset; no selection processes the whole list. Up/Down changes the order of one selected item.
- Resize one or many photos: width/height in pixels, keep ratio, optional enlargement. Zero means original/automatic. Keep ratio fits inside the supplied bounds.
- Export PNG, JPEG (quality), BMP, TIFF or still GIF. Outputs use new names; original files stay intact. JPEG/BMP flatten transparency onto white.
- Color: Original, binary BlackWhite, Grey, high-contrast monochrome Chrome, Sepia and Blueprint. Use Preview to inspect the current settings.
- Combine two or more photos into animated GIF: set milliseconds per frame and loop. Mixed sizes fit on the first frame's canvas, with white padding. Uses the same FFmpeg available to video tools.
- Print preview: A4/Letter, portrait/landscape, rows/columns, margin in mm and optional filenames. One image per page uses 1 column × 1 row. Batch layout paginates automatically. Print opens the Windows printer dialog.
- Animated GIF and multipage TIFF inputs use their first frame. Exports create new pixel images and do not preserve source EXIF metadata.

Validation: Release build passed; 323 assertions passed including actual animated GIF frames/timing, image conversion round trips, pixel resize, tone checks, calibrated perimeter/area with undo, rendered print cells, and existing capture/recording/UI checks. Native Photo Tools preview and print layout were inspected. Physical printer output was not tested.
