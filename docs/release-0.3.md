# SnapMark 0.3 — คู่มือเครื่องมือครบ 7 หมวด

เวอร์ชันนี้ส่งมอบเครื่องมือพื้นฐานทุกหมวดตามคำขอล่าสุด รวมภาพประกอบจาก history,
ลำดับ Step ที่แก้ได้ และวิดีโอ local MP4 ส่วน cloud / account / direct Teams–ACC API พักไว้
รายการ 120 หัวข้อยังเป็น roadmap ไม่ใช่ทั้งหมดที่ทำเสร็จแล้ว

| หมวด | ใช้ได้ในเวอร์ชันนี้ |
|---|---|
| Edit | Select, Ctrl+click เลือกหลายวัตถุ, move, resize ด้วยมุมขวาล่าง, duplicate, erase/delete, bring front/send back, crop, zoom/pan, ขยาย canvas |
| Draw | Arrow, Line, rectangle/rounded rectangle, circle/ellipse, triangle, polygon ตามจุด, regular polygon 3–32 ด้าน, continuous line/loop, free form, open/filled dot, solid/gradient plane |
| Highlight & Redact | Highlight, Blur, Pixelate, opaque Solid cover; ปรับ radius/block size |
| Label & Callout | Text, Number/letter Step, leader callout, note box; แก้ label ย้อนหลัง |
| Review | Revision cloud แบบกรอบและ freeform, 29 design stamps, custom stamp |
| Dimension & Measure | Dimension label กรอกเอง, calibration จากเส้นอ้างอิง, distance, polyline length, area, angle |
| Symbols | north, section, elevation, detail, grid, level, centerline, axis, break, slope, check, cross, warning, issue |

## เส้นและรูปทรง

0.3.3: Opacity แสดงตัวเลข % และกรอก 0–100 ได้ slider เปลี่ยนตามช่องกรอก
สีหลักและสีที่สองของ gradient กรอก HEX เองได้ (`#RRGGBB` หรือ `#RGB`)
ปุ่ม ◉ เปิด Color wheel (Hue/Saturation) พร้อม Brightness, HEX, RGB preview และสีล่าสุดใน session
ปุ่ม swatch เป็นตัวเลือกด่วน ไม่จำกัดรายการสีที่ใช้ได้; Cancel ไม่เปลี่ยนสี
เมื่อ Select วัตถุ ช่อง options โหลดค่าของวัตถุนั้น แล้วแก้ค่าและกด Apply style to selection
ข้าง Opacity เพื่อใช้กับวัตถุที่เลือก; Ctrl+Z คืนค่าเดิมได้
toolbar แยก Tools/Output/History/View/Insert/Canvas และ options แยกกรอบตามหน้าที่
Pantone ไม่รวมในรุ่นนี้; ผู้ผลิตมี [สิทธิ์สำหรับนำ library ไปใช้ในซอฟต์แวร์](https://www.pantone.com/hk/en-hk/license/software-and-web)

0.3.1: ปุ่ม **Select วัตถุ · V** อยู่บน toolbar ตลอดทุกหมวด คลิกแล้วเลือก/ลากวัตถุ
Ctrl+click เลือกหลายวัตถุ; ลาก handle มุมขวาล่างเพื่อ resize หรือกด V เพื่อเข้า Select

0.3.2: Edit → Eraser ลากซ้าย→ขวาเป็นกรอบฟ้า ลบวัตถุที่ bounds อยู่ในกรอบทั้งหมด
ลากขวา→ซ้ายเป็นกรอบเขียวเส้นประ ลบวัตถุที่ bounds ตัดกับกรอบ (Line ตรวจเส้นจริง)
วัตถุที่จะลบมีกรอบสีแดงระหว่างลาก; ปล่อยเมาส์ลบทั้งชุด, Esc ยกเลิก, Ctrl+Z คืนทั้งชุด
คลิกโดยไม่ลากยังลบวัตถุเดียวได้ ไม่ลบ pixels ของภาพต้นฉบับ

เลือกหมวด Draw แล้วเลือกเครื่องมือ สีและ Stroke คือความหนาเส้น
Line type มี Solid / Dash / Dot / DashDot / LongDash; endpoint เลือก None / OpenDot / FilledDot / Arrow
Shape fill มี None / Solid / Gradient โดย gradient ใช้สีหลักและสีที่สอง
Opacity ปรับ 0–100% และ Rotation ใส่องศา จากนั้นวาด หรือกด Apply style to selection เพื่อแก้ของเดิม
Properties แสดงตามเครื่องมือที่เลือก; เลื่อนแผง options เพื่อดูรายการด้านล่าง

- Polygon / Continuous line / Length / Area / freeform cloud: คลิกจุดต่อเนื่อง แล้ว Enter หรือ double-click เพื่อจบ; Esc ยกเลิก
- Continuous line และ Free form มี Close loop; Free form วาดโดยลากเมาส์
- Regular polygon: เลือกจำนวนด้านแล้วลาก; Circle บังคับสัดส่วนเท่ากัน
- Open/Filled dot: คลิกตำแหน่ง; Text size กำหนดขนาดจุด
- Angle: คลิก 3 จุด โดยจุดที่สองเป็น vertex
- Shift ระหว่างลาก Line / Arrow / Dimension / Distance ใช้ snap มุม 45°

## Step และ Callout

กด N หรือเลือก Label & Callout → Number ตั้งช่อง Next label เป็นเลขเริ่มต้น เช่น 12
แล้วคลิกวาง จะได้ 12, 13, 14… เปิด Alphabetic แล้วใส่ A หรือ AA เพื่อไล่ตัวอักษร (Z ต่อ AA)
Callout ใช้ sequence เดียวกัน แต่ลากจาก bubble ไปจุดที่ต้องการชี้
เลือกวัตถุแล้ว F2 หรือ double-click เพื่อเปลี่ยนเลข/ตัวอักษรย้อนหลังได้อิสระ
การแก้ label เก่าไม่ renumber วัตถุอื่น และ Undo ไม่ย้อนตัวนับ Next label; ตั้งช่องนี้เองเมื่อต้องการเริ่มใหม่

## นำภาพเก่ามาประกอบ

ใน editor กด Insert history → เลือกรูป → Insert into editor หรือ double-click
อีกทางคือ History → Copy selected image แล้ว Ctrl+V ใน editor
Insert file ใช้ภาพจากเครื่อง และ Ctrl+V รับภาพ clipboard
ภาพที่แทรกเป็นวัตถุที่ย้าย/resize/rotate/delete/duplicate และ undo ได้
Canvas size ขยายพื้นที่เพื่อวางภาพข้างกันได้; ภาพแทรกอยู่ใต้ markup และ redaction
History เก็บภาพต้นฉบับของ capture; clipboard ที่ copy จาก editor เป็นภาพรวม markup แล้ว

## วัดและเก็บงานแก้ต่อ

Calibrate: ลากเส้นที่รู้ระยะจริง แล้วใส่ค่าพร้อมหน่วย เช่น `3500 mm`
Distance / Length / Area จะคำนวณตาม scale ของเอกสาร; Angle แสดงองศา
Dimension เป็น label ที่ผู้ใช้กรอกเอง แยกจาก Distance ที่คำนวณ
การวัดอิง pixels ของภาพและ scale เดียว ไม่ได้อ่านโมเดล BIM หรือแก้ perspective

Ctrl+S บันทึก PNG และ Ctrl+C copy ภาพที่รวมผลทั้งหมด
Save editable project บันทึก `.snapmark`; เปิดกลับจากหน้าหลักหรือ `--open` เพื่อแก้ต่อ
ไฟล์นี้มีต้นฉบับภาพ, geometry/style, crop, calibration และภาพแทรก จึงเก็บข้อมูลก่อน redaction อยู่ด้วย
ใช้ PNG/clipboard ที่ flatten แล้วสำหรับส่งภาพที่ปิดบังข้อมูล
ยังไม่มี autosave/crash recovery

## Video capture

กด Record video ที่หน้าหลัก/เมนู tray → เลือก Region / Current monitor / All monitors,
10/15/24/30 fps และ cursor → เลือกปลายทาง MP4 → เลือกพื้นที่ → countdown 3 วินาที
หยุดด้วย Stop and save MP4, Ctrl+Shift+F10 หรือเมนู tray Stop video
ถ้าไม่มีที่วางหน้าต่างควบคุมนอกพื้นที่อัด จะซ่อนก่อนเริ่ม; ใช้ shortcut/tray เพื่อหยุด
Stop จะ finalize คลิปแล้วเปลี่ยนชื่อ temporary file ไปปลายทาง

ต้องมี `ffmpeg.exe` ที่ PATH, ข้าง exe หรือในโฟลเดอร์ `tools` ข้าง exe; portable ไม่รวม FFmpeg
เครื่องนี้พบ FFmpeg และทดสอบบันทึก/หยุด/ถอดรหัส MP4 จริงผ่านแล้ว
Implementation ใช้ [FFmpeg gdigrab](https://ffmpeg.org/ffmpeg-devices.html#gdigrab)
รุ่นนี้เป็นวิดีโออย่างเดียว ไม่มี microphone/system audio, GIF, trim หรือ video editor
ถ้าติดปัญหา finalize จะแจ้งข้อผิดพลาดและเก็บไฟล์ชั่วคราวไว้ให้กู้

## การตรวจสอบและขอบเขต

Release build ผ่าน 0 warnings / 0 errors; runner ผ่าน 99 assertions พร้อม real capture/recording
ทดสอบ render เครื่องมือใหม่, gradient/dash/opacity, image insertion/redaction,
calibration/commands, alphabetic sequence และ native project round trip แบบเทียบ pixels
UI ทดลองกับภาพสังเคราะห์: circle, polygon, Step เริ่ม 12 และ history insertion
มีชุด symbol พื้นฐาน; custom SVG packs, editable vertices, OCR, scrolling capture,
custom hotkeys, audio, crash recovery และ cloud ยังไม่อยู่ในรุ่นนี้
