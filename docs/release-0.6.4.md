# SnapMark 0.6.4 — Adjust the recording frame before starting

ลำดับใหม่: **เลือกโหมด → ลากพื้นที่ → ปรับกรอบ → Use this area / Enter → เลือกไฟล์ MP4 → ยืนยันเริ่ม → นับถอยหลัง 3 วินาที → อัด**

- ปล่อยเมาส์แล้วกรอบยังรอให้ปรับ ไม่เริ่มอัดอัตโนมัติ
- ลาก handle 8 จุด (มุมและกึ่งกลางขอบ) เพื่อปรับขนาด หรือลากภายในเพื่อย้ายทั้งกรอบ
- ขนาดแสดงเป็น physical pixels; กรอบอยู่ภายใน desktop และมีขนาดขั้นต่ำ 2×2 px
- ลูกศรย้ายกรอบ, Shift+ลูกศรปรับขนาด, Ctrl เพิ่มระยะทีละ 10 px; Esc ยกเลิก
- Current monitor / All screens มีกรอบเริ่มต้นให้ปรับได้; Pick window ก็ปรับหลังเลือกหน้าต่างได้
- เลือก path บันทึกหลังยืนยันกรอบ จึงไม่ต้องเลือกไฟล์ก่อนลากพื้นที่
- เมื่อหยุดอัด แสดงและโฟกัส editor เดิม หากไม่มี editor เปิดอยู่จะแสดงหน้าหลัก
- ยังใช้ editor เดิมระหว่างอัดได้ตาม 0.6.3; ภาพในกรอบอัดจะเป็นส่วนหนึ่งของวิดีโอ

Validation: **282 assertions passed**, including eight resize handles, minimum/crossing limits, negative monitor coordinates, explicit overlay confirmation, returning to an existing editor/main window, real MP4 recording with concurrent editing, and existing capture/history/properties/plugin tests. Native UI verified dragging 320×240 then resizing the same frame to 400×300 without canceling, followed by the Save As dialog only after Next. Hardware audio was not retested.
