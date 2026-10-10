# SnapMark 0.6.5 — Recycle history thumbnails and resize their strip

- กด **×** บนภาพย่อใน History ของ editor เพื่อส่ง capture นั้นไป **Windows Recycle Bin**
- คลิก thumbnail เพื่อเลือก/preview แล้วกด **Delete** ได้เช่นกัน; Delete ใน canvas ยังลบ annotation ตามเดิม
- ส่ง PNG และ sidecar metadata / OCR cache ไปถังขยะพร้อมกัน กู้คืนได้จาก Windows Recycle Bin
- หลังลบ รายการ History ใน editor ที่เปิดอยู่จะ refresh และกลับมางานเดิมหากลบภาพที่กำลัง preview
- ภาพและ annotation ที่เปิดหรือแทรกไว้ใน canvas ยังอยู่ครบ
- ลาก **เส้นสีด้านบนแถบ History** ขึ้น/ลงเพื่อปรับความสูง ภาพย่อขยายตามและรักษาสัดส่วนภาพ
- Hide / Show thumbnails ยุบหรือกลับไปความสูงก่อนยุบได้

Uses [Windows IFileOperation recycling flags](https://learn.microsoft.com/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperation-setoperationflags); no permanent-delete fallback is implemented. Only app-owned captures and matching sidecars within the configured history folder are accepted; linked files and paths outside that folder are rejected.

Validation: **291 assertions passed**, including real Windows recycling and verification of restorable Recycle Bin records for the PNG and both sidecars, failures/path validation, preservation of current markup, proportional thumbnail resizing, and existing capture/video/editor/plugin checks. Hardware audio was not retested.
