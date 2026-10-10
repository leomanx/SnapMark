# SnapMark 0.6.3 — Use the existing editor while recording video

แก้ workflow อัดวิดีโอที่ซ่อนหน้าต่าง editor เดิมไว้จนกว่าจะหยุดอัด ตอนนี้ซ่อนเฉพาะช่วงเลือกพื้นที่ แล้วแสดงกลับก่อน countdown / เริ่มอัด จึงเลือกเครื่องมือ แก้ annotation, แทรกภาพ, Undo/Redo และส่งออกจากหน้าต่างเดิมได้ระหว่างอัด ไม่ต้องเปิด SnapMark อีก instance

- ยกเลิกเลือกพื้นที่หรือเริ่มอัดไม่สำเร็จ: แสดงหน้าต่างเดิมกลับ
- หยุดอัด: ไม่เปิดหน้าต่างที่ผู้ใช้ปิดหรือซ่อนระหว่างอัดขึ้นใหม่
- การเลือกพื้นที่ส่งผลกลับหลังออกจาก handler เพื่อให้ capture overlay ปิดก่อนขั้นตอนถัดไป
- ภาพที่ปรากฏภายในกรอบอัด รวมถึง editor ที่นำเข้าไปในกรอบ จะเป็นส่วนหนึ่งของวิดีโอ

Validation: **258 assertions passed**. Real MP4 integration checks verify the same editor stays visible/enabled, tool buttons and image insertion/undo work while the encoder runs, and stopping finalizes the video. Includes cancellation/one-time window restoration and existing capture, history, properties, video and plugin tests. Hardware audio was not retested.
