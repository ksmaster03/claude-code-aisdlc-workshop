# OpsTrack

ระบบแจ้งซ่อมในโรงงาน .NET 8 + EF Core 8 (SQLite) + ASP.NET Core Web API + หน้าเว็บใน wwwroot

## โครงสร้าง (ห้ามข้ามชั้น)
- `src/OpsTrack.Data` DAL: DbContext, entities, repositories
- `src/OpsTrack.Business` BL: services, rules (SlaCalculator, ShiftCalendar, Guard, IClock)
- `src/OpsTrack.Api` API + UI: controllers เรียก service เท่านั้น ห้ามแตะ DbContext ตรง
- `tests/` xUnit (Business.Tests, Api.Tests) · `e2e/` Playwright
- แผนที่ไฟล์ละเอียดอยู่ใน @docs/REPO_MAP.md

## คำสั่ง
- build: `dotnet build OpsTrack.sln`
- test ทั้งหมด: `dotnet test OpsTrack.sln`
- test เฉพาะส่วน: `dotnet test --filter "Area=Sla"` (ดูแผนที่ใน tests/test-map.json)
- รันเว็บ: `dotnet run --project src/OpsTrack.Api` แล้วเปิด http://localhost:5080

## กติกาที่ต้องทำทุกครั้ง
- ก่อนสร้าง helper, service หรือ extension ใหม่ ให้ค้นของเดิมก่อน และบอกว่าเจออะไร
  (ของที่มีอยู่แล้ว: Guard, IClock, SlaCalculator, ShiftCalendar, NotificationClient)
- งานที่แตะเกินหนึ่งชั้น ให้เริ่มใน Plan mode แผนต้องมี: ไฟล์ที่จะแก้, ของเดิมที่จะใช้, ผลกระทบ, เทสต์ที่จะเพิ่ม
- ห้ามแก้เทสต์เดิมให้ผ่าน ถ้าเทสต์ผิดจริง ให้หยุดแล้วอธิบายเหตุผลก่อน
- เขียนเทสต์จาก acceptance criteria ก่อนแก้โค้ด แล้วรันจนผ่าน
- การเขียนข้อมูลที่ต้องเช็คสถานะ ใช้คำสั่งเดียวแบบมีเงื่อนไข (ExecuteUpdateAsync + WHERE) แล้วเช็คจำนวนแถว
- ห้ามแตะ `db/scripts/released/` ถ้าต้องเปลี่ยน schema ให้เพิ่มไฟล์ใหม่ใน `db/scripts/pending/`
- ห้ามใส่คีย์ รหัสผ่าน หรือ URL ลับในโค้ด ใช้ configuration เท่านั้น
- ข้อความที่ผู้ใช้เห็นเป็นภาษาไทย ชื่อในโค้ดเป็นภาษาอังกฤษ

## เมื่อไม่แน่ใจ
- requirement ไม่ชัด: ถามก่อน อย่าเดา และเขียนสมมติฐานไว้ในไฟล์ REQ
- งานยาวหลาย session: เขียนสรุปลง `docs/handoff/<งาน>.md` ตามแม่แบบ `docs/handoff/TEMPLATE.md` ก่อน /clear

## Compact instructions
เมื่อสรุปบทสนทนา ให้เก็บ: เป้าหมายของงาน, ไฟล์ที่แก้แล้ว, การตัดสินใจและเหตุผล, เทสต์ที่ยังแดง, สิ่งที่เหลือ
