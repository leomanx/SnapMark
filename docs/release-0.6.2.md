# SnapMark 0.6.2 — History previews in the editor viewport

หลัง capture แถบ **History / ภาพย้อนหลัง** จะแสดง thumbnail ด้านล่าง editor อัตโนมัติ โดยอ่านจาก History folder ที่ตั้งไว้ใน Settings

- คลิก thumbnail เพื่อดูภาพใหญ่ในพื้นที่กลางของหน้าต่างเดียวกัน
- **Back to current edit / Esc** กลับมางานเดิมพร้อม annotation และ undo stack
- **Insert into current image** แทรกภาพย้อนหลังเป็นวัตถุในงานปัจจุบัน ใช้ Select เพื่อย้าย/ปรับขนาด และ Undo ได้
- **Ctrl+C** ขณะ preview คัดลอกภาพย้อนหลังที่แสดง
- **Hide / Show thumbnails** ยุบหรือเปิดแถบภาพย่อ และ **Refresh** อ่านรายการใหม่

Preview เป็นการดูภาพ capture ต้นฉบับ งานที่กำลังแก้จะเก็บไว้ โดยปุ่มแก้ไขและส่งออกงานเดิมพักไว้จนกดกลับมางาน หากต้องการแก้ภาพเก่าเป็นงานแยก ใช้ History จากหน้าหลัก → Open editor ตามเดิม

โหลดภาพและ thumbnail นอก UI thread, จำกัด thumbnail 160 px และปล่อยภาพ preview เมื่อกลับมางาน/ปิดหน้าต่าง การกลับระหว่างโหลดหรือไฟล์หายไม่แทรกภาพเก่าค้างโดยผิดพลาด

Validation: **250 assertions passed** across core/workflow, desktop capture, synthetic video, plugin and UI model tests; includes preservation of current markup, undoable history insertion, missing files and cancellation of pending previews. Hardware audio remains deferred as requested.
