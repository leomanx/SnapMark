# SnapMark 0.6.12

<img src="src/SnapMark.App/Assets/SnapMark.png" width="80" alt="SnapMark logo">

**A portable Windows capture and technical markup app for architecture/design review.**

PrintScreen → select a region → annotate → copy or save. Built with C#, WPF and .NET 8.
Personal portfolio project by **Teerapat Chumrassomboon**. Status: **Portable Beta**.
Source-available: free to use for company/professional work; software resale is restricted.
See [LICENSE](LICENSE), [known issues](docs/known-issues.md) and [quick start](docs/quickstart.md).

### Highlights

- Seven markup categories: shapes, blur/redaction, labels, review stamps, calibrated measurements and drawing symbols.
- Editable objects with vertices, mirror/rotate, style controls, undo/redo and saved projects.
- Searchable/renameable capture history and image composition in the editor.
- Region MP4 recording, photo batch processing/print layouts, local OCR and synced team folders.
- Portable/self-contained Windows x64 app; FFmpeg is optional and external for video/GIF.

Download the portable ZIP from this repository's **Releases** page, extract it,
and run `SnapMark.exe`. Source builds require the .NET 8 SDK on Windows.
The ZIP excludes personal history/recovery. The executable is currently unsigned.

เครื่องมือพื้นฐานครบ 7 หมวด พร้อม image composition และ local video capture
ไม่มี account / telemetry; เลือกเซฟสำเนาไปโฟลเดอร์ทีม Google Drive / OneDrive ที่ sync อยู่ได้

Latest: [0.6.12 · Editor improvements](docs/release-0.6.12.md).
Release preparation: [five-group audit](docs/release-readiness.md),
[quick start](docs/quickstart.md), [portable updates](docs/portable-release.md),
[known issues](docs/known-issues.md), [exporter plugins](docs/plugins.md).
History controls: [0.6.5 · Recycle history thumbnails and resize their strip](docs/release-0.6.5.md).
Recording frame: [0.6.4 · Adjust recording frame before starting and return to editor](docs/release-0.6.4.md).
Recording/editing: [0.6.3 · Existing editor remains usable while recording video](docs/release-0.6.3.md).
History preview: [0.6.2 · History thumbnails and previews in the editor viewport](docs/release-0.6.2.md).
Symbols/properties: [0.6.1 · Symbol labels/font size and Select object properties](docs/release-0.6.1.md).
Previous: [0.6 · Direct tools, recording boundary, text background, history OCR and team folders](docs/release-0.6.md).
Roadmap: [0.5 · Eight work groups, audio/resolution verification and remaining scope](docs/release-0.5.md).

## เริ่มใช้

แตก ZIP แล้วเปิด `SnapMark.exe` บน Windows x64; ไม่ต้องติดตั้ง .NET เพิ่ม
เก็บ `portable.flag` ข้าง exe เพื่อเก็บ settings/history ใน `data` ข้างโปรแกรม

1. PrintScreen → ลากกรอบ → ได้ภาพใน editor
2. เลือกหมวดและเครื่องมือ แล้ว annotate
3. Ctrl+C copy ภาพรวมผล หรือ Ctrl+S export PNG/JPEG/BMP/TIFF/PDF
4. Save editable project เก็บ `.snapmark` เพื่อเปิดกลับมาแก้ต่อ

ปิดหน้าหลักแล้วแอปยังอยู่ใน tray; Exit ในเมนู tray เพื่อออก
Shift+PrintScreen จับพื้นที่แล้ว copy โดยไม่เปิด editor; Esc ยกเลิก capture

## ฟีเจอร์รุ่นนี้

| หมวด | เครื่องมือ |
|---|---|
| Edit | Select/multi-select, move/resize, duplicate/delete/eraser, layer order, crop, zoom/pan |
| Draw | Arrow/Line, rectangle/rounded, circle/ellipse, triangle, polygon, regular polygon, polyline/loop, free form, open/filled dot, plane |
| Highlight & Redact | Highlight, Blur, Pixelate, opaque Solid cover |
| Label & Callout | Text, editable number/letter steps with custom start, leader, note |
| Review | Rectangle/freeform revision cloud, 29 design stamps and custom stamp |
| Dimension & Measure | Manual dimension, calibration, distance, polyline length, area, angle |
| Symbols | 14 basic drawing markers including north/section/elevation/grid/level/issue |

Line patterns, weight, endpoint markers, fill/gradient, opacity, rotation and style presets.
Insert history / clipboard image / image file as an editable image object; expand canvas.
Capture region/monitor/all monitors/visible active window/repeat region, bounded local history.
Video capture Region/Monitor/All screens/visible window region → local MP4, cursor effects, 10/15/24/30 fps.
WASAPI System + Microphone capture options; native physical pixels or max 720/1080/1440/2160, quality presets.
The user confirms video with JLAB microphone audio works (2026-10-05); simultaneous system + mic
and long-session synchronization still need confirmation. Video setup includes Refresh/Test audio.
Video requires external `ffmpeg.exe`; Settings → Video encoder → Browse selects its file.
Auto searches beside exe, in `tools`, then PATH. FFmpeg is not bundled.
เลือก audio input สำหรับวิดีโอได้; มี Trim video / Export GIF ในหน้าหลัก
เพิ่ม autosave/recovery, hotkey/delay/naming settings, personal presets, path vertices,
SVG symbol library, Compare, PDF import/export, local OCR, guided scrolling capture และ Diagnostics
0.4.2: แก้ popup WPF ตอนปิด Editor โดยให้ถาม/บันทึก/ปิดหลัง event Closing จบ;
Cancel กลับไปแก้ต่อได้ และคงการบันทึก recovery ก่อนปิด.

0.4.1: Settings → Storage มี Browse / Default และช่องแก้ path แยก History, Video/Trim/GIF,
Image/PDF export, Editable projects และ Recovery. หน้า History แสดง source folder.
เปลี่ยน history/output มีผลทันที; เปลี่ยน recovery มีผลหลัง restart. ไฟล์เดิมไม่ย้ายอัตโนมัติ.

ดู [คู่มือ 0.4 · 10 local workflow jobs และข้อจำกัด engine](docs/release-0.4.md)
และ [เครื่องมือครบ 7 หมวด](docs/release-0.3.md)

0.5 adds precise/freeform capture, typography/eyedropper/grid, Ctrl+K, project profiles, history search,
ROI OCR/auto scrolling, configurable GIF, Light/System theme, local GUI installer and exporter plugins.
Browser extension source is included for manual setup; hardware/Thai/runtime checks remain open.

## Shortcuts

| Shortcut | Action |
|---|---|
| PrintScreen / Ctrl+Shift+S | Region → editor |
| Shift+PrintScreen | Region → clipboard |
| Alt+PrintScreen | Visible active window |
| Ctrl+PrintScreen | Monitor under cursor |
| Ctrl+Shift+R | Repeat last region |
| Ctrl+Shift+V | History |
| Ctrl+Shift+F10 | Stop/finalize video recording |
| Ctrl+C / Ctrl+S / Ctrl+V | Copy result / save PNG / insert clipboard image |
| Ctrl+Z / Ctrl+Y | Undo / redo |
| Ctrl+D / Delete | Duplicate / remove selected objects |
| Ctrl+click | Multi-select |
| Arrow / Shift+Arrow | Move 1 / 10 pixels |
| V / A / R / T / H / N | Select / Arrow / Rectangle / Text / Highlight / Step |
| B / P / X / S / L | Blur / Pixelate / Solid cover / Stamp / Line |
| Enter / Esc | Finish path / cancel gesture |
| F2 / double-click | Edit selected label |
| Ctrl+wheel / middle-drag | Zoom / pan |

Hotkeys are configurable in Settings; conflicts are reported. Region button works if PrintScreen is occupied.

Select วัตถุ · V is always visible on the editor toolbar.
Opacity displays an editable 0–100 percentage beside the synchronized slider.
Primary and gradient colors support custom HEX, HSV color wheel, brightness,
visual swatches and recent session colors. Select loads the object's style;
change values and use Apply style to selection beside Opacity to update it.
Toolbar commands and sidebar options are grouped with labeled sections.
Pantone library is not bundled; see the release guide for its licensing source.
Edit → Eraser: drag left-to-right to delete fully enclosed object bounds, or
right-to-left to delete crossing bounds (actual stroke for Line). Matching objects
highlight during drag; release deletes the batch, Esc cancels, Ctrl+Z restores it.
Click without dragging still erases a single object.

## Build and verify

```powershell
dotnet build SnapMark.sln -c Release
dotnet run --project tests/SnapMark.Tests -c Release -- --capture --recording
dotnet run --project src/SnapMark.App -c Release -- --portable
./scripts/publish-portable.ps1
```

Dependency-free console test runner exits nonzero on failure. `--capture` tests desktop APIs.
`--recording` records a small temporary region, finalizes and decodes it; requires FFmpeg.
Latest verification and remaining hardware checks: [release audit](docs/release-readiness.md).
The prior 0.6.12 UI-model run passed 407 assertions; older counts in release guides
describe those versions rather than the current suite.
Close test app processes before rebuilding/publishing their loaded binaries.

Normal data: `%APPDATA%/SnapMark`. Portable data: `data` beside exe. `--tray` starts minimized.
Open images or native projects: `SnapMark.exe --open "C:\path\drawing.snapmark"`.

History stores original captures. Disable Keep history before capturing images you do not
want retained; existing history is preserved. Auto-copy initially copies the original;
disable auto-copy if only the redacted result should enter clipboard. Ctrl+C/save PNG flatten
all effects. `.snapmark` retains originals and editable effects; share flattened PNGs for redaction.
Blur/pixelate obscure detail; Solid cover provides opaque black coverage.

## Current scope

This implements the requested first set in all seven categories, not the full 120-topic blueprint.
Additional local jobs are implemented in 0.4: recovery, configurable capture settings,
personal presets, vertices/basic SVG library, compare, PDF, installed-language OCR,
guided scrolling, audio/trim/GIF and manual local installation/update.
Automatic scrolling, WASAPI loopback and a basic exporter plugin API are implemented;
broader page/device validation, background capture and tool/capture/share plugin APIs remain.
Team folders supports existing Google/OneDrive sync plus provider share pages.
Cloud OAuth/account/direct Teams–ACC integration is postponed. Paste copied images into destinations manually.
Measurements use one user-calibrated image scale, not BIM model data or perspective correction.
Symbols are basic markup graphics. Layer ordering applies within vector/image layers;
redaction stays above embedded images. PNG/JPEG/BMP/TIFF/PDF exports are flattened.

GDI capture reads visible desktop pixels; occluding windows appear. Protected content,
secure desktop, minimized/offscreen windows and HDR fidelity are unsupported.
Mixed-DPI/cross-monitor hardware and long recordings need broader testing.

See [architecture](docs/architecture.md), [roadmap](docs/design-roadmap.md),
[120-topic catalog](docs/blueprint-catalog.md), [validation](docs/validation.md).
Copyright © 2026 Teerapat Chumrassomboon. See [LICENSE](LICENSE):
source-available, free use including companies and paid professional work;
selling SnapMark or modified versions requires separate permission. This is not MIT
or an OSI-approved open-source license. Third-party components retain their own terms.
