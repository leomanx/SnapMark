# SnapMark — แผนพัฒนาสำหรับงานออกแบบ / Architecture / BIM

Current 0.6.12: [Editor improvements](release-0.6.12.md).
Release preparation and evidence: [five-group audit](release-readiness.md).
0.6.11: [Ribbon branding](release-0.6.11.md).
0.6.5: [Recycle history thumbnails and resize their strip](release-0.6.5.md).
0.6.4: [Adjust recording frame and return to editor](release-0.6.4.md).
0.6.3: [Use the existing editor while recording](release-0.6.3.md).
0.6.2: [History previews in the editor viewport](release-0.6.2.md).
0.6.1: [Symbol labels/font size and selected object properties](release-0.6.1.md).
0.6 workflows: [Direct tools, recording boundary, text background, history OCR and team folders](release-0.6.md).
Roadmap baseline: [Eight work groups, delivered workflows and remaining scope](release-0.5.md).

เป้าหมายเวอร์ชันแรก: **PrintScreen → ลากกรอบ → ภาพ → annotate → Ctrl+C / Ctrl+S**

ให้ความสำคัญกับความเร็วและ markup งานออกแบบก่อน ความสำเร็จวัดจาก workflow ที่จบได้จริง
สถานะล่าสุด 0.3: P1-A/B/C ส่งมอบแล้ว รวมเครื่องมือพื้นฐานครบ 7 หมวด
ตามคำขอล่าสุดเร่ง measurement/symbols, image composition, `.snapmark` และ video MP4 มาทำแล้ว
อัปเดต 0.4: autosave/recovery, personal presets, vertices/SVG, compare, PDF,
local OCR, guided scrolling และ audio/trim/GIF มี implementation แล้ว
ดู [สถานะ 10 local jobs และข้อจำกัดจริง](release-0.4.md). ตารางเดิมด้านล่างเป็น historical plan;
hardware matrix, advanced capture และ optional ecosystem ยังเหลือ
ตารางด้านล่างเป็นแผนเดิมก่อนขยายขอบเขต ดูสถานะและวิธีใช้ที่ [คู่มือ 0.3](release-0.3.md)

ดู [หมวดเครื่องมือ](tool-categories.md) และ [การจัดครบ 120 หัวข้อ](blueprint-catalog.md)

## ลำดับพัฒนา

| ลำดับ | กลุ่มงาน | ส่งมอบ | เกณฑ์จบ |
|---|---|---|---|
| P0 · ฐานที่ต้องรักษา | Capture → Editor → Output | Hotkey, frozen region, DPI/monitor, clipboard, PNG, undo/redo, error handling | จับภาพและส่งต่อได้; Esc ยกเลิกได้; export ตรง preview |
| P1-A · ทำต่อทันที | หมวดเครื่องมือ + Blur + Stamp | Registry/category UI, Blur, Pixelate, solid redaction, stamp งานออกแบบ, preset สี/น้ำหนักเส้น | หาเครื่องมือเร็ว; ปิดบัง ROI ได้; วาง/แก้ stamp ได้; copy/save รวมผลครบ |
| P1-B · MVP รีวิวแบบ | Shapes + Callout + Revision cloud | Line, ellipse, rounded rectangle, freehand, numbered callout/leader, revision cloud, select/move/resize/style | จบ review แบบหนึ่งภาพด้วยลูกศร เมฆ revision และ RFI |
| P1-C · MVP dimension | Dimension + calibration | Dimension label กรอกเอง, pixel dimension, calibrate สองจุด, หน่วย/scale ต่อภาพ | แยกค่ากรอกเองกับค่าที่วัดชัดเจน; undo/redo และ export ได้ |
| P2-A · เก็บงานแก้ต่อ | Document persistence | `.snapmark`, versioned annotation JSON, autosave/recovery, modified-state | เปิดกลับมาแก้ vector/effect/stamp/crop ต่อได้ |
| P2-B · ออกแบบลึกขึ้น | Measures + Symbols + Compare | Distance/polyline/area/angle, symbol packs, RFI presets, compare/diff | ตรวจ before/after และวัดภาพ calibrate แล้วได้ |
| P2-C · ลดจังหวะใช้งาน | Productivity | ปรับ hotkey, delay, naming/folder, drag/drop, saved presets, PDF page import | ใช้กับ drawing/project สะดวกขึ้นโดย workflow หลักยังสั้น |
| P3 · ขยายเมื่อใช้จริง | OCR / scrolling | Local OCR อังกฤษ/ไทย, selected ROI, scroll stitching/browser capture | ทดสอบ workflow ภาษาและหน้าเลื่อนจริง |
| P4 · ภายหลัง | Recording / ecosystem | MP4/GIF/audio, plugin SDK, installer/update, direct integrations | ทำหลัง MVP งานออกแบบนิ่งและมีความต้องการชัดเจน |

P1-A → P1-B → P1-C คือ **Design MVP รุ่นแรก** จากนั้นทำ P2-A ก่อนขยาย symbol packs จำนวนมาก
เพื่อบันทึกงานที่แก้ต่อได้ ไม่ต้องรอ OCR หรือ recording

## ขอบเขต Design MVP

**ต้องมี:** region/quick capture, editor แบ่งหมวด, Arrow/Line, Rectangle/Ellipse, Text,
Highlight, Blur/Pixelate/solid redaction, Number/Callout, Revision Cloud, design stamps,
Dimension พื้นฐาน, markup presets, select/move/resize, undo/redo, crop, clipboard และ PNG

**ฟีเจอร์รอง:** repeat region, monitor/active-window capture, open/paste image,
settings เล็กน้อย, portable และแจ้งผล copy/save

**พักไว้:** library UI ขนาดใหญ่, cloud sync, account, template workflow, project dashboard,
การจัดภาพย้อนหลังจำนวนมาก และ direct API ของ Teams/ACC; recording MP4 พื้นฐานทำแล้วใน 0.3

History ที่มีอยู่เป็นฟีเจอร์รอง ไม่เปิดก่อน capture และไม่ขยายเป็น library ใน MVP
clipboard คือช่องทางส่งภาพไป Teams/ACC: ผู้ใช้ paste ในปลายทางเอง
การอัปโหลด/ส่งผ่าน API อัตโนมัติเป็นงานภายหลัง

## Stamp / preset ที่ให้ก่อน

| ชุด | ตัวอย่าง |
|---|---|
| Review / coordination | MARKUP, FOR REVIEW, FOR COORDINATION, COORDINATE, CHECK, VERIFY |
| Issue / clarification | RFI, RFI-01, TBC, ISSUE, CLASH, MISSING INFORMATION |
| Design action | ADD, MOVE, DELETE, REVISE, ALIGN, MATCH, OFFSET |
| Drawing context | NOT TO SCALE, REFERENCE ONLY, EXISTING, PROPOSED, REV-01 |
| Discipline | ARCH, STRUCT, MEP, ID, LANDSCAPE |

P1 มี stamp แบบข้อความ/กรอบที่แก้สี ขนาดและข้อความได้; rotation และ custom packs เพิ่มใน P2
สีเริ่มต้น: Red review, Yellow highlight, Blue coordination, Green proposal, Technical mono
discipline colors แก้ได้ ไม่ถือเป็นมาตรฐานของทุกโครงการ

## ข้อกำหนดเครื่องมือใหม่

- Preview / clipboard / PNG ใช้ renderer เดียวกัน ผลปิดบังต้องถูก flatten ลงภาพที่ส่งออก
  ไม่พ่วง original image เข้า clipboard/export
- Blur/Pixelate ลดรายละเอียด แต่ไม่รับรองว่ากู้ข้อความไม่ได้; มี solid redaction สำหรับปิดทับข้อมูล
- Original ที่ใช้แก้ไขยังอาจอยู่ใน history / `.snapmark`; เพิ่มทางเลือกไม่เก็บ history ใน P1-A
- Manual dimension เป็น label ไม่ใช่การวัด; calibrated measure ต้องเก็บ reference length/หน่วย/scale
- Symbol เป็น editable annotation ของเราเอง; ไม่ใช้ proprietary assets และไม่อ้างว่าเป็น BIM object
- Tool ใหม่รองรับ selection, command history, crop, zoom และ copy/save ตั้งแต่เพิ่ม

## สถานะปัจจุบัน

| กลุ่ม | ทำแล้ว | ยังเหลือ |
|---|---|---|
| Capture | Region/monitor/all screens/visible window/repeat, window selection, precision/freeform, hotkey/delay settings | Background/occluded capture, broader mixed-DPI/HDR hardware testing |
| Editor / design | 7 categories, shapes/freeform/paths, callouts/steps/revision clouds/stamps, Select/marquee, vertices, mirror/rotation, editable properties, presets, reorder/resize UI | Broader usability/performance QA and richer symbol packs |
| Dimension / measure | Calibration, distance, loop/polyline length, area, angle, centered labels and text/background formatting | Multi-scale/perspective or BIM geometry is outside current scope |
| Output / history | Image/PDF, editable projects, recovery, composition, thumbnails/rename/search/recycle, configured storage, photo batch/print/GIF tools | Clean-machine release QA and long-session validation |
| OCR / scrolling | Installed-language OCR and history index/search, ROI, guided/automatic scrolling, browser extension source | Thai OCR, wider real-page compatibility, extension browser runtime QA |
| Video / audio | Adjustable frame, visible boundary, native/max-height MP4, WASAPI system/mic, trim/GIF, FFmpeg file preference | Final Both-mode listening and long-duration synchronization checks |
| Ecosystem / release | Sync team folders/provider share pages, exporter DLL v1, portable manifest/checksums, custom source-available license | Cloud OAuth/direct Teams–ACC, tool/capture/share SDK, signing and public support/security channels |

**Roadmap ใหญ่ยังไม่ครบทั้งหมด**: Design MVP และงาน local หลักมี implementation แล้ว
แต่ catalog 120 หัวข้อรวม architecture และแนวทางระยะยาว ไม่ใช่ checklist ของฟีเจอร์ 120 ตัว
ห้ามใช้จำนวน assertions แทนการยืนยัน hardware/clean-machine tests ที่ยังไม่ทำ

## วิธีเพิ่ม scope

เพิ่มเครื่องมือย่อยลงหมวดเดิมก่อนสร้างหมวดใหม่ ระบุ owner category, priority,
acceptance workflow และ dependency ทุก feature ไม่เพิ่มปุ่ม toolbar ระดับบนหนึ่งปุ่มต่อ symbol
หัวข้อหลักสถาปัตยกรรมและ phase summary ใน blueprint ไม่ใช่ฟีเจอร์ที่ต้องทำซ้ำ
