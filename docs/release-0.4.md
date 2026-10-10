# SnapMark 0.4 · Local workflow jobs

## 0.4.2 · Editor close fix

ย้ายขั้นตอนถาม/บันทึก/ปิด Editor ออกจาก event Closing ผ่าน Dispatcher.
ป้องกัน Close ซ้อนและ Save dialog ขณะ WPF กำลังปิด แม้ task บันทึกจะจบทันที.
Cancel คืนหน้าต่างให้แก้ต่อ; ระหว่าง final recovery write หยุดแก้ document จนบันทึกเสร็จ.
Recording window ใช้การเลื่อนขั้นตอนหยุด/ปิดออกจาก Closing เช่นกัน.
Build Release ผ่านไม่มี warning/error; ชุดทดสอบเดิมผ่าน 143 assertions.
ทดสอบ UI จริง: ปิด Editor ที่ยังไม่แก้ไข, งานค้าง → Cancel → วาดต่อได้,
และงานค้าง → Keep recovery → ปิดโดยไม่มี alert พร้อมไฟล์ recovery ที่มี annotation ครบ.

## 0.4.1 · Storage folders

Settings → Storage แก้ path / Browse / Default ได้แยก 5 โฟลเดอร์:
Image/PDF export (`Pictures\SnapMark`), Video/Trim/GIF (`Videos\SnapMark`),
Editable projects (`Documents\SnapMark`), Capture history และ Autosave recovery.
History/recovery default อยู่ใน `data\history` / `data\recovery` ของ portable
หรือ `%APPDATA%\SnapMark\history` / `recovery` ของ installed mode.
ค่า Default ของ history/recovery ยังติดตามตำแหน่ง data เมื่อย้าย portable folder.
หน้า History แสดง source folder เพื่อดูว่าภาพมาจากไหน.

History/output paths มีผลทันที รวม Save ของ editor ที่เปิดอยู่.
Recovery path มีผลหลัง restart เพื่อรักษา session ที่เปิดอยู่.
การเปลี่ยน path ไม่ย้าย/ลบไฟล์เดิม; History limit จัดการเฉพาะชื่อไฟล์ capture ที่ SnapMark สร้าง
และไม่ลบ PNG อื่นในโฟลเดอร์ที่เลือก. เก็บเฉพาะภาพต้นฉบับของ capture ที่เปิด Keep history.

ต่อจากเครื่องมือพื้นฐานครบ 7 หมวดใน 0.3.3 รุ่นนี้ทำงาน local อีก 10 กลุ่มตามลำดับที่ตกลง
Cloud/account/sync และ direct Teams/ACC integration ยังพักไว้; ใช้ clipboard ได้ตามเดิม

| Job | ฟีเจอร์ที่เพิ่ม | ขอบเขต / วิธีใช้ |
|---|---|---|
| 1 | Autosave / recovery | บันทึก editable snapshot เมื่อมีการแก้ไข ทุก 5 วินาที; Recover editable work เปิดกลับพร้อม original, annotations, image layers และ calibration |
| 2 | Capture / output settings | แก้ hotkeys, ปิด shortcut ด้วยช่องว่าง, delay 0–10 วินาที, export folder, filename `{date}_{time}_{kind}` |
| 3 | Personal presets | Save current style / stamp เก็บสี เส้น endpoint fill opacity rotation stamp และค่า polygon; ตั้งชื่อและเรียกใช้ข้ามการเปิดโปรแกรม |
| 4 | Vertices / custom symbols | Select → Edit path vertices → ลากจุดสีส้ม; Ctrl+Z คืน geometry เดิม. Symbols → Import SVG → เก็บสัญลักษณ์ส่วนตัวเป็น PNG object |
| 5 | Compare | Reference image + current flattened editor image: overlay, side by side, difference threshold 0–255, เปอร์เซ็นต์ pixels ที่เปลี่ยน และ save difference PNG |
| 6 | PDF / export formats | Open image / PDF เลือกหน้าและ DPI 72–300; Ctrl+S เลือก PNG/JPEG/BMP/TIFF/flattened PDF |
| 7 | Local OCR | Editor → OCR text เลือกภาษาที่มี Windows OCR pack; อ่านภาพหลัง annotate/crop แล้วแก้ข้อความหรือ Copy text |
| 8 | Scrolling capture | เลือกพื้นที่ content, เลื่อนหน้าลงเองให้เหลื่อมอย่างน้อย 20%, Next frame, Undo last frame, Finish → editor |
| 9 | Video audio / trim / GIF | Record video เลือก No audio หรือ DirectShow audio input; Trim video / Export GIF เลือกช่วงวินาทีและสร้างไฟล์ใหม่ |
| 10 | Diagnostics / local install & update | Diagnostics แสดง monitor geometry, DPI หน้าต่าง, OCR/FFmpeg และเวลา render; package manifest SHA-256 + install-local.ps1 ติดตั้งต่อผู้ใช้และอัปเดตจาก package local |

## Recovery

Settings → Autosave มีผลกับ editor ที่เปิดใหม่. Close editor เลือก Keep recovery / Discard / Cancel;
Save editable project จะบันทึก `.snapmark` แล้วปลด recovery ของ session นั้น.
Export รูปไม่ปลดสถานะงานแก้ไข เพราะภาพ flattened เปิดกลับมาแก้วัตถุไม่ได้.
Tray → Exit จะ flush recovery ก่อนปิด; ถ้าบันทึกไม่ได้จะคงแอปเปิดไว้.
เมื่อ autosave ปิด ต้อง save หรือ close editor ก่อน Exit.

Recovery เก็บ original และ editable assets เช่นเดียวกับ `.snapmark`; ใช้ flattened export
เมื่อส่งภาพที่ต้องปิดทับข้อมูล. หลัง crash อาจสูญเสียการแก้ไขช่วงไม่เกินรอบ autosave ล่าสุด
และไม่สามารถรับประกันการกู้เมื่อ disk เต็ม/ไฟล์เสีย.

## ขอบเขตของ engine

- SVG รองรับ path, rect, circle, ellipse, line, polyline, polygon และ group ของไฟล์ที่ผู้ใช้มีสิทธิ์ใช้. ไม่รองรับ transform, gradient definitions, filters, clipping, text, external assets หรือ CSS classes; flatten ก่อน import. เก็บผล raster สูงสุด 512px ไม่ใช่ vector editor เต็มรูปแบบ.
- Compare ต้องขนาด pixels เท่ากัน; ไม่มี auto alignment และ diff เป็น pixel difference ไม่ใช่การแปลความหมายแบบ BIM.
- PDF import เป็นภาพหน้าเดียว ไม่อ่าน PDF vector objects; export PDF เป็นภาพที่ flatten แล้ว ไม่เก็บ layers. PDF limit 256 MB, render limit 64 MP / 16000px.
- OCR อยู่ในเครื่องโดยใช้ [Windows OCR](https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr.ocrengine). แสดงเฉพาะภาษาที่ติดตั้ง; ภาษาไทยต้องมี Thai OCR language pack. เครื่องทดสอบมี en-US จึงตรวจ English OCR จริงได้; Thai ยังไม่ได้ทดสอบบนเครื่องนี้. ภาพใหญ่จะย่อตามขนาดที่ engine รับได้.
- Scroll stitching เป็น guided manual capture แนวตั้ง. เลือก content ที่ไม่มี fixed header/footer, animation หรือ horizontal movement. ถ้า overlap ไม่ชัดเจนจะปฏิเสธและให้เลื่อนน้อยลง; content ซ้ำ/พื้นที่ว่างมากอาจทำให้จับ overlap ผิด ควรตรวจภาพก่อนใช้งาน. จำกัด 50 frames / 256 MB ของ frame pixels / 64 MP / 16000px.
- Video/audio/GIF ใช้ external FFmpeg ใน PATH หรือข้าง exe. ไม่ bundle FFmpeg. เสียงผ่าน [DirectShow inputs](https://ffmpeg.org/ffmpeg-devices.html#dshow); system audio ต้องมี Stereo Mix / loopback input ที่เปิดให้ DirectShow ใช้ได้. รุ่นนี้ยังไม่มี native WASAPI loopback. ไม่เปิดไมโครโฟนอัตโนมัติ ค่าเริ่มต้น No audio.
- GIF 12 fps, กว้างสูงสุด 960px, ไม่มีเสียง; trim MP4 re-encode และเก็บ audio stream ที่มี. ช่วง export สูงสุด 1 ชั่วโมง, source ต้องต่างจาก output.

## ติดตั้ง / อัปเดตจากไฟล์ในเครื่อง

Portable เปิด exe ได้เลย; `portable.flag` เก็บข้อมูลใน `data` ข้าง exe.
Package มี `release-manifest.json` ตรวจ SHA-256 เพื่อจับไฟล์ขาด/เปลี่ยน ไม่ใช่ digital signature
หรือการรับรองผู้สร้าง package. ใช้ package จากแหล่งที่เชื่อถือได้.

รัน PowerShell ในโฟลเดอร์ package:

```powershell
.\install-local.ps1
```

ไม่ใช้ administrator. ติดตั้งที่ `%LOCALAPPDATA%\SnapMark\version-0.4.0` และสร้าง Start Menu shortcut.
เพิ่ม `-DesktopShortcut` ถ้าต้องการ Desktop shortcut; `-NoShortcuts` ติดตั้งไฟล์อย่างเดียว.
รุ่น installed เก็บข้อมูลที่ `%APPDATA%\SnapMark` ต่างจาก portable; script ไม่ย้าย portable history อัตโนมัติ.
อัปเดตโดยรัน script จาก package รุ่นใหม่ จะสร้าง version folder ใหม่และเปลี่ยน shortcut.
ปิดโปรแกรมรุ่นเดิมก่อนเปิดรุ่นใหม่; เก็บรุ่นเก่าไว้สำหรับ rollback. ไม่มี network update checker.

## Validation และงาน hardware ที่ยังต้องตรวจ

Build Release ไม่มี warning/error. Tests ครอบ recovery atomic save และ background save จาก decoded image,
hotkey/naming validation, personal presets, vertex undo, SVG import, pixel diff,
image formats, PDF round-trip ด้วย Windows PDF engine, English OCR, exact scroll stitching,
real desktop capture/MP4 decode และ trim/GIF จากคลิป synthetic ที่มี audio.

Diagnostics เป็นข้อมูลของจอที่เชื่อมต่อจริงเท่านั้น. Mixed-DPI monitor switching/disconnect,
HDR/protected windows, Thai OCR และการ sync เสียงจากไมโครโฟนจริงยังต้องตรวจบน hardware/ภาษาเป้าหมาย.
จึงยังไม่อ้างว่า job 10 ผ่าน hardware matrix ครบทุกชนิด.
