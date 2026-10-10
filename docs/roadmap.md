# Roadmap (superseded)

แผนฉบับนี้เป็นลำดับเดิมก่อนผู้ใช้เน้นงานออกแบบ ให้ใช้
[Design roadmap](design-roadmap.md), [หมวดเครื่องมือ](tool-categories.md)
และ [catalog ครบ 120 หัวข้อ](blueprint-catalog.md) เป็นลำดับพัฒนาปัจจุบัน

## Foundation implemented

0. Solution architecture with separate Core, Capture, Editor, Export and App assemblies.
1. Tray, global hotkeys, conflict status, single-instance startup.
2. Frozen per-monitor region overlays, physical pixel selection, Shift constraint, Esc.
3. Capture → clipboard, including quick-copy mode and retry handling.
4. Editor canvas with zoom, fit, pan and selection/move.
5. Arrow, Rectangle, Text. Also Highlight and Number.
6. Command-based undo/redo, delete, duplicate, movement and crop.
7. PNG export through the same vector renderer as clipboard copy.

Additional foundation features: repeat region, monitor/all-screen capture, visible
active window, original-image history, settings, image import/paste and portable mode.

## Next: editor durability and productivity

- Versioned `.snapmark` documents, annotation-session autosave and recovery.
- Resize handles, precise multiline text bounds, style editing, multi-select, layering.
- Blur/pixelate with defined privacy semantics, freehand and additional shapes.
- Configurable hotkeys, delay capture, auto naming/folders, presets, image drag/drop.
- Broader mixed-DPI / monitor-disconnect testing; magnifier and keyboard region adjustment.
- Window hover selection and modern capture backend where GDI is insufficient.

## Technical / BIM identity

Dimension → scale calibration → revision cloud → RFI stamp → compare/diff.
Keep measurements separate from image presentation; disclose calibration/unit metadata.

## Later independent modules

Local English/Thai OCR, scrolling/stitching, recording/audio, plugin SDK, optional OS sharing.
Each module needs its own acceptance workflow before being presented as available.
