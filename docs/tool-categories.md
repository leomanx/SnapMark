# หมวดเครื่องมือ SnapMark

0.3 เปิดใช้เครื่องมือพื้นฐานครบ 7 หมวดแล้ว รวม Dimension/Measure และ Symbols
ดูรายการส่งมอบและวิธีใช้จริงที่ [คู่มือ 0.3](release-0.3.md)
ตารางลำดับด้านล่างเก็บแผนจัดหมวดเดิม; รายการ interface/contract บางส่วนเป็นแนวทางขยายในอนาคต

| หมวด | เครื่องมือย่อย | ลำดับ |
|---|---|---|
| จัดการภาพและวัตถุ · Edit | Select, Move, Resize, Delete, Duplicate, layer order, Crop, Zoom, Pan | ฐานเดิม + P1 |
| เส้นและรูปทรง · Draw | Arrow, Line, Rectangle, Rounded rectangle, Ellipse, Polygon, Polyline, Freehand | P1; polygon/polyline ที่ซับซ้อน P2 |
| เน้นและปิดบัง · Highlight & Redact | Marker/fill highlight, Blur, Pixelate, Solid redaction | P1-A |
| ข้อความและหมายเหตุ · Label & Callout | Text, numbered step/callout, letter callout, leader, note box | P1-B |
| ตรวจแบบและสถานะ · Review | Revision cloud rectangle/freeform, Stamp, RFI tag, revision tag | Stamp P1-A; Cloud/Callout P1-B |
| มิติและการวัด · Dimension & Measure | Manual dimension label, pixel distance, Calibration, scaled distance, polyline length, area, angle | Dimension P1-C; measures P2 |
| สัญลักษณ์งานออกแบบ · Symbols | North arrow, section/elevation/detail, grid bubble, level, centerline, axis, break, slope arrow | P2; catalog รองรับตั้งแต่ P1-A |

Capture / Repeat / Open / Paste และ Copy / Save เป็นคำสั่งระดับแอป
สี ความหนา font fill dash opacity เป็น properties ของ tool/selection ไม่เป็นหมวดเครื่องมือแยก

## โครงหน้าต่าง

```text
Top:     Capture / Repeat | Undo / Redo | Copy / Save
Left:    7 หมวด → เครื่องมือย่อยของหมวดที่เลือก
Center:  Image + editable annotations
Options: Tool/selection properties + style preset
Footer:  Image size / Zoom / Scale / Status
```

จำหมวดและเครื่องมือที่ใช้ล่าสุดระหว่าง capture เครื่องมือใช้บ่อยเข้าถึงได้ภายใน 1–2 การกระทำ
favorites ใน P2 เก็บ reference ไป tool เดิม การวาง annotation ไม่ต้องเลือก project/library ก่อน

## Catalog ที่รองรับการเพิ่มย่อย

```text
Category → Tool → Variant → Properties + Preset / Symbol definition
```

- Draw → Arrow → Single head / Double head / Technical leader
- Draw → Shape → Rectangle / Rounded / Ellipse / Polygon
- Review → Stamp → RFI / MARKUP / FOR COORDINATION / Custom text
- Review → Revision cloud → Rectangle / Freeform
- Symbols → Drawing marker → Section / Elevation / Detail
- Dimension → Linear → Manual label / Pixel / Calibrated

ใช้ StampTool เดียวกับหลาย definitions ไม่สร้าง class แยกทุกคำ เช่นเดียวกับ symbol family
เริ่ม registry ภายใน Editor โดยไม่ต้องรอ Plugin SDK

| Contract/model | หน้าที่ |
|---|---|
| ToolCategoryDefinition | Stable ID, ชื่อ, ลำดับ, icon |
| ToolDefinition | Stable tool ID, category ID, display name, shortcut, gesture factory, options schema |
| ToolVariantDefinition | Geometry/behavior variant ของ tool เดิม |
| StylePreset | Stroke/fill/opacity/font/dash และค่าเริ่มต้นที่แก้ได้ |
| StampDefinition | ข้อความ, border, padding, typography, default style; ไม่มี mouse logic |
| SymbolDefinition | Vector geometry, anchor points, editable labels, defaults; ไม่มี Win32 dependency |
| Annotation | Object ID, tool/variant ID, geometry, style, text, optional measurement metadata |

UI สร้างรายการจาก registry แล้ว ไม่วน enum ทั้งหมดเป็นปุ่มระดับบน
ขั้นต่อไป: IEditorTool แยก gesture/state ของแต่ละ family ออกจาก UI; Serialization ใช้ stable ID/schema version
ไม่ใช้ display name เป็น key Renderer และ hit-test ใช้ geometry ร่วมกันเท่าที่ทำได้

ลำดับ render: base image → ROI effects/redaction → vectors → selection UI
export ข้าม selection UI และ clip ตาม crop; เพิ่ม consistency tests สำหรับ Blur/Stamp/Cloud/Dimension

## Symbol packs ภายหลัง

- Architecture: north, section/elevation/detail, room/zone label, door/window tag, grid, level
- Coordination: clash/issue pin, issue ID, discipline label, route/direction arrow
- Drawing graphics: centerline, break, slope, datum, reference arrow, revision delta
- General: check, cross, question, warning, circle/square/triangle markers

Custom SVG packs อยู่ P2 หลังมี import validation และ versioning สัญลักษณ์เป็น markup บนภาพ
ไม่ได้เชื่อมข้อมูล CAD/BIM โดยอัตโนมัติ
