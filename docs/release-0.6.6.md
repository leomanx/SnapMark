# SnapMark 0.6.6 — Compact, grouped home screen

หน้าแรกแยกเป็น 4 กลุ่ม: **Capture**, **Video**, **Files & History** และ **Settings & Extensions**

- ปุ่มกว้างพอดีกับข้อความแทนช่องสองคอลัมน์ที่ยืดเต็มพื้นที่
- WrapPanel จัดปุ่มขึ้นบรรทัดใหม่ตามความกว้างหน้าต่าง; หน้าต่างขนาดเล็กเลื่อนดูได้
- Region / Record video ใช้สีเข้มเพื่อให้เห็นคำสั่งหลักชัดเจน
- ปุ่มลัดอยู่บรรทัดเล็กใต้ชื่อปุ่ม และอัปเดตตาม Settings โดยไม่แทนที่ชื่อคำสั่ง
- Pick window อยู่ในกลุ่ม Capture พร้อมคำอธิบายการชี้เลือกหน้าต่าง
- ลดขนาดหัวเรื่องและช่องว่าง; ทุกกลุ่มแสดงครบในขนาดเปิดปกติ 860×630 DIPs
- แสดงเลขเวอร์ชันข้างชื่อ SnapMark เพื่อแยกรุ่นเก่าและรุ่นใหม่

Validation: Release build passed with zero warnings/errors. Native UI inspected at the default window size: all four groups and all existing actions are visible. This release changes home layout and shortcut presentation; no additional tests were added for the reversible layout change.
