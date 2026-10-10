# Catalog ครบ 120 หัวข้อจาก blueprint

จัดตาม **กลุ่มหลักหนึ่งกลุ่มต่อหัวข้อ** เพื่อเช็ก coverage; implementation อาจเกี่ยวข้องหลายกลุ่ม
เลขหัวข้อและชื่อเดิมคงไว้เพื่อย้อนกลับไปเอกสารต้นทางได้
ตารางนี้บอกลำดับพัฒนา **ไม่ได้หมายความว่าหัวข้อนั้นทำครบแล้ว**
สถานะ implementation ดู [design-roadmap.md](design-roadmap.md)

## ภาพรวม 10 กลุ่มงาน

| กลุ่ม | จำนวนหัวข้อ | ทำเมื่อไร | ความหมาย |
|---|---:|---|---|
| A · ทิศทางผลิตภัณฑ์และขอบเขต | 11 | P0 · ใช้เป็นเกณฑ์ตัดสินใจ | เป้าหมาย ความเร็ว local-first positioning และขอบเขต MVP; ไม่ใช่เครื่องมือ UI |
| B · ฐานระบบและความถูกต้อง | 26 | P0 · รักษาและเสริมตาม feature | Dependency, document/annotation, commands, coordinate/DPI, rendering, logging และ tests |
| C · การจับภาพ | 12 | พื้นฐาน P0; delay P2; capture ขั้นสูง P3 | Region/monitor/window/repeat/freeze เป็นแกน; magnifier/snap/element capture ค่อยเพิ่ม |
| D · Editor และ annotation ทั่วไป | 17 | P1 · Blur และหมวด UI ก่อน | เส้น รูปทรง ข้อความ emphasis/redaction และการจัดการวัตถุ; color picker/ruler/guide เพิ่มหลัง core |
| E · งานออกแบบ / Architecture / BIM | 9 | P1 · Stamp/Cloud/Callout/Dimension; measure/compare P2 | เครื่องมือสร้าง identity: preset, stamp, dimension, revision cloud, callout, calibrated measure และ compare |
| F · รับภาพและส่งภาพ | 10 | Clipboard/PNG P0; naming/PDF/import P2; direct share P4 | Copy/save และ open/paste เป็นแกน; Teams/ACC ผ่าน clipboard ก่อน direct integration |
| G · Workflow / การเก็บงาน / UI รอง | 15 | Tray/hotkey P0; document/recovery P2-A; profiles/library ภายหลัง | Settings/workflow, history, persistence, feedback, profiles; ไม่ให้ library ขวาง capture |
| H · ความสามารถขั้นสูงที่ใช้ engine เพิ่ม | 6 | OCR/scroll P3; recording P4; auto-redaction หลัง OCR | OCR, stitching/browser scrolling, video/audio/GIF และ OCR-assisted redaction |
| I · Packaging และ ecosystem | 5 | Portable P0 (มีแล้ว); plugin/installer/update P4 | แพ็กใช้งานและขยายระบบเมื่อ feature หลักนิ่ง |
| J · Phase summaries จาก blueprint เดิม | 9 | อ้างอิงกับแผนใหม่; ไม่ใช่งานเพิ่มซ้ำ | เป็นลำดับเดิมที่สรุป feature ข้างต้น; ปรับ design tools ให้มาก่อน OCR/scroll/recording |

P0 = ฐานที่จำเป็น/หลักการที่รักษาตลอด; P1 = Design MVP; P2 = durability/งานออกแบบเพิ่มเติม;
P3 = OCR/scroll; P4 = optional ecosystem ภายหลัง หมายเลข phase เดิมไม่ใช้เป็นลำดับใหม่

## การตีความหัวข้อกว้าง

- #18 Blur/Pixelate และ #46 Stamp ย้ายขึ้น P1-A เป็นงานเร่งด่วน
- #42 Architecture/BIM Tools แตกเป็น dimension P1-C และ calibrated measures ขั้นสูง P2
- #21 Selection เริ่ม resize/style ใน P1; multi-select/rotate/layer tooling ที่ซับซ้อนเพิ่ม P2
- #24 Export: PNG เป็น P0 ที่มีแล้ว; JPEG/BMP เพิ่มเมื่อจำเป็น, PDF/WebP ภายหลัง ไม่ต้องครบทุก format เพื่อจบ MVP
- #27 History: มี bounded history แล้ว; P4 คือการขยาย/search/library ไม่ใช่รื้อของเดิมมาทำใหม่
- #41/#47 Presets: ให้ design/discipline presets ง่าย ๆ P1; editor และบันทึก custom preset เพิ่ม P2
- #43 Measure แยกจาก manual dimension: ต้อง calibration และ unit metadata ก่อน
- #64 Share: clipboard เป็น P0; direct Teams/ACC/API ไม่ต้องใช้ใน MVP และเป็น P4
- #65/#66 Privacy/Security เป็นหลักการ P0; opt-out history และ redaction UX ทำร่วมกับ P1-A
- #68/#69/#70 เป็น auxiliary design tools เพิ่ม P2 ไม่แย่งลำดับ Blur/Stamp
- #76 PDF Import แทรก P2 เพราะสัมพันธ์กับงานแบบ แม้ไม่ต้องมีใน MVP จับ screenshot
- #85 ขอบเขต MVP เดิมปรับให้รวม Stamp/Revision Cloud/Dimension ตามแผนใหม่
- #95/#96 เป็นแนวทางเลือก dependency ไม่ใช่รายการที่ต้องติดตั้งทุก package
- #98 Interfaces เพิ่มตาม module ที่ใช้งานจริง ไม่สร้าง implementation เปล่าให้ดูเหมือน feature เสร็จ
- #111/#112 เป็นการตัดสินใจผลิตภัณฑ์/asset boundary ไม่ใช่ feature UI
- #115/#116 Profiles/Revit integration เป็น optional หลังชุด design tool ใช้งานทั่วไปได้
- #86–94 เป็นสรุป phases ที่ซ้ำกับ feature chapters ไม่สร้าง task ซ้ำ

## A · ทิศทางผลิตภัณฑ์และขอบเขต

เป้าหมาย ความเร็ว local-first positioning และขอบเขต MVP; ไม่ใช่เครื่องมือ UI

| หัวข้อเดิม | ชื่อเดิม | ลำดับใหม่ |
|---:|---|---|
| 1 | Product Goal | P0 |
| 2 | Core UX Philosophy | P0 |
| 3 | Target Platform | P0 |
| 58 | Performance Targets | P0 |
| 65 | Privacy Model | P0 |
| 66 | Security | P0 |
| 85 | MVP Scope | P0 |
| 111 | Licensing | P0 |
| 112 | Intellectual Property Boundary | P0 |
| 113 | Product Differentiator | P0 |
| 114 | Architecture Workflow Example | P0 |

## B · ฐานระบบและความถูกต้อง

Dependency, document/annotation, commands, coordinate/DPI, rendering, logging และ tests

| หัวข้อเดิม | ชื่อเดิม | ลำดับใหม่ |
|---:|---|---|
| 4 | High-Level Architecture | P0 |
| 5 | Repository Structure | P0 |
| 10 | Capture Result Model | P0 |
| 11 | Editor Architecture | P0 |
| 12 | Annotation Object Model | P0 |
| 20 | Undo / Redo | P0 |
| 40 | Settings Architecture | P0 |
| 54 | Rendering Pipeline | P0 |
| 55 | Image Pipeline | P0 |
| 56 | DPI Awareness | P0 |
| 57 | Multi-Monitor | P0 |
| 95 | Dependency Strategy | P0 |
| 96 | Suggested NuGet | P0 |
| 97 | Dependency Injection | P0 |
| 98 | Core Interfaces | P0 |
| 99 | Event System | P0 |
| 100 | Logging | P0 |
| 101 | Error Handling | P0 |
| 102 | Testing Strategy | P0 |
| 103 | Coordinate System | P0 |
| 104 | Editor Document Model | P0 |
| 105 | Tool State Machine | P0 |
| 106 | Architecture Principle | P0 |
| 107 | Suggested Namespace | P0 |
| 117 | Capture Metadata | P2 |
| 120 | Final Recommended Product Layers | P0 |

## C · การจับภาพ

Region/monitor/window/repeat/freeze เป็นแกน; magnifier/snap/element capture ค่อยเพิ่ม

| หัวข้อเดิม | ชื่อเดิม | ลำดับใหม่ |
|---:|---|---|
| 6 | Core Modules | P0 |
| 7 | Region Capture | P0 |
| 8 | Window Capture | P0 |
| 9 | Full Screen / Monitor Capture | P0 |
| 28 | Quick Capture Mode | P0 |
| 30 | Repeat Last Region | P0 |
| 31 | Delayed Capture | P2 |
| 36 | Cursor Capture | P4 / optional |
| 71 | Screen Freeze | P0 |
| 72 | Magnifier | P2 |
| 73 | Snap Detection | P3 |
| 74 | UI Automation Capture | P3 |

## D · Editor และ annotation ทั่วไป

เส้น รูปทรง ข้อความ emphasis/redaction และการจัดการวัตถุ; color picker/ruler/guide เพิ่มหลัง core

| หัวข้อเดิม | ชื่อเดิม | ลำดับใหม่ |
|---:|---|---|
| 13 | Annotation Tools | P1 |
| 14 | Arrow Tool | P1 |
| 15 | Text Tool | P1 |
| 16 | Shape Tool | P1 |
| 17 | Highlight Tool | P1 |
| 18 | Blur / Pixelate | P1 |
| 19 | Numbered Steps | P1 |
| 21 | Selection System | P1 |
| 22 | Crop System | P1 |
| 29 | Editor Mode | P0 |
| 51 | Canvas / Workspace | P0 |
| 52 | Keyboard-First Workflow | P0 |
| 53 | Styling Toolbar | P1 |
| 68 | Color Picker | P2 |
| 69 | Ruler Tool | P2 |
| 70 | Crosshair / Screen Guide | P2 |
| 84 | UI Layout | P1 |

## E · งานออกแบบ / Architecture / BIM

เครื่องมือสร้าง identity: preset, stamp, dimension, revision cloud, callout, calibrated measure และ compare

| หัวข้อเดิม | ชื่อเดิม | ลำดับใหม่ |
|---:|---|---|
| 41 | Preset System | P1 |
| 42 | Architecture/BIM Tools | P1 |
| 43 | Measure Tool | P2 |
| 44 | Revision Cloud | P1 |
| 45 | BIM Callout Tool | P1 |
| 46 | Stamp System | P1 |
| 47 | Discipline Presets | P1 |
| 49 | Compare Tool | P2 |
| 50 | Image Difference Detection | P2 |

## F · รับภาพและส่งภาพ

Copy/save และ open/paste เป็นแกน; Teams/ACC ผ่าน clipboard ก่อน direct integration

| หัวข้อเดิม | ชื่อเดิม | ลำดับใหม่ |
|---:|---|---|
| 23 | Clipboard | P0 |
| 24 | Export Engine | P0 |
| 25 | Auto Naming | P2 |
| 26 | Save Location | P2 |
| 64 | Share Integrations | P4 / optional |
| 76 | PDF Import | P2 |
| 77 | Drag & Drop | P2 |
| 78 | Paste Image | P0 |
| 79 | Open Existing Image | P2 |
| 80 | Context Menu Integration | P2 |

## G · Workflow / การเก็บงาน / UI รอง

Settings/workflow, history, persistence, feedback, profiles; ไม่ให้ library ขวาง capture

| หัวข้อเดิม | ชื่อเดิม | ลำดับใหม่ |
|---:|---|---|
| 27 | Capture History | P4 / optional |
| 37 | Hotkey Manager | P0 |
| 38 | System Tray | P0 |
| 39 | Startup Behavior | P4 / optional |
| 48 | Project Mode | P4 / optional |
| 59 | Autosave Crash Recovery | P2 |
| 60 | Native Project Format | P2 |
| 61 | Annotation JSON | P2 |
| 81 | Notification System | P0 |
| 82 | Sound Feedback | P4 / optional |
| 83 | Theme | P4 / optional |
| 115 | Revit-Specific Future Feature | P4 / optional |
| 116 | App Profiles | P4 / optional |
| 118 | Searchable History | P4 / optional |
| 119 | Command Palette | P4 / optional |

## H · ความสามารถขั้นสูงที่ใช้ engine เพิ่ม

OCR, stitching/browser scrolling, video/audio/GIF และ OCR-assisted redaction

| หัวข้อเดิม | ชื่อเดิม | ลำดับใหม่ |
|---:|---|---|
| 32 | Scrolling Capture | P3 |
| 33 | OCR | P3 |
| 34 | Screen Recording | P4 / optional |
| 35 | GIF Recording | P4 / optional |
| 67 | Auto Redaction | P3 |
| 75 | Scrolling Webpage Capture | P3 |

## I · Packaging และ ecosystem

แพ็กใช้งานและขยายระบบเมื่อ feature หลักนิ่ง

| หัวข้อเดิม | ชื่อเดิม | ลำดับใหม่ |
|---:|---|---|
| 62 | Plugin Architecture | P4 / optional |
| 63 | Plugin Folder | P4 / optional |
| 108 | Portable Version | P0 |
| 109 | Installer | P4 / optional |
| 110 | Auto Update | P4 / optional |

## J · Phase summaries จาก blueprint เดิม

เป็นลำดับเดิมที่สรุป feature ข้างต้น; ปรับ design tools ให้มาก่อน OCR/scroll/recording

| หัวข้อเดิม | ชื่อเดิม | ลำดับใหม่ |
|---:|---|---|
| 86 | Phase 1 — Capture Core | อ้างอิง phase เดิม |
| 87 | Phase 2 — Editor Core | อ้างอิง phase เดิม |
| 88 | Phase 3 — Advanced Annotation | อ้างอิง phase เดิม |
| 89 | Phase 4 — Productivity | อ้างอิง phase เดิม |
| 90 | Phase 5 — Architecture Tools | อ้างอิง phase เดิม |
| 91 | Phase 6 — OCR | อ้างอิง phase เดิม |
| 92 | Phase 7 — Scrolling | อ้างอิง phase เดิม |
| 93 | Phase 8 — Recording | อ้างอิง phase เดิม |
| 94 | Phase 9 — Plugin SDK | อ้างอิง phase เดิม |
