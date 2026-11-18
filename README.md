# OpsTrack

> **repo แลบของเวิร์กช็อป AI-SDLC ด้วย Claude Code** · branch `lab/pr-review` ใช้ในแลบ L8 · ชุดมาตรฐานทีมและสคริปต์ตรวจเครื่องอยู่ใน [`workshop/`](workshop/README.md)

OpsTrack เป็นระบบแจ้งซ่อมเครื่องจักรในโรงงาน **ที่สร้างขึ้นสำหรับฝึกอบรมเท่านั้น** ใช้แทนระบบงานปฏิบัติการของทีม (ไม่ใช่โค้ดจริงของบริษัทใด ข้อมูลทั้งหมดเป็นข้อมูลสมมติ)

หน้าตาและโครงสร้างตั้งใจให้เหมือนระบบ .NET รุ่นเก่าทั่วไป คือแบ่งชั้น DAL, BL, API และ UI มีเทสต์ไม่มาก และมีจุดบกพร่องซ่อนอยู่หลายจุดที่แลบจะพาไปหาและแก้

## สิ่งที่ต้องมี

| เครื่องมือ | รุ่น | หมายเหตุ |
|---|---|---|
| .NET SDK | 8 ขึ้นไป | โปรเจกต์ตั้งเป็น net8.0 และ `RollForward=Major` เครื่องที่มีแค่ .NET 9 หรือ 10 ก็รันได้ |
| Node.js | 20 ขึ้นไป | ใช้กับเทสต์ E2E (Playwright) และ MCP จำลอง |
| Git | รุ่นใดก็ได้ | |
| Claude Code | รุ่นล่าสุด | ใช้ในแลบ |

ฐานข้อมูลใช้ SQLite ไม่ต้องติดตั้ง SQL Server ไฟล์ฐานข้อมูลถูกสร้างเองตอนเปิดโปรแกรมครั้งแรก

## เปิดระบบ

```
dotnet build
dotnet run --project src/OpsTrack.Api
```

เปิดเบราว์เซอร์ที่ http://localhost:5080

- ไฟล์ฐานข้อมูลอยู่ที่ `src/OpsTrack.Api/opstrack.db` ถ้าอยากเริ่มข้อมูลใหม่ ให้ปิดโปรแกรมแล้วลบไฟล์นี้ (ต้องลบด้วยเมื่อมีการเปลี่ยนโครงสร้างตาราง เพราะระบบสร้างตารางด้วย `EnsureCreated`)
- ตอนเปิดครั้งแรกระบบใส่ข้อมูลตัวอย่างให้: 3 ไลน์ (SMT-1, SMT-2, FATP-1) เครื่องจักร 11 เครื่อง งานซ่อม 30 งาน

### ผู้ใช้จำลอง

ระบบนี้ไม่มีหน้า login ผู้เรียกบอกตัวเองผ่าน header สองตัว (ใช้ในแลบเท่านั้น ระบบจริงต้องใช้ Windows authentication หรือ Entra ID)

| header | ค่า |
|---|---|
| `X-User` | ชื่อผู้ใช้ภาษาอังกฤษ เช่น `op.malee` |
| `X-User-Role` | `Operator` (ผู้แจ้ง), `Technician` (ช่าง), `Supervisor` (หัวหน้า) |

หน้าเว็บมีช่องเลือกผู้ใช้และบทบาทที่มุมขวาบน ผู้ใช้ในข้อมูลตัวอย่าง: `op.malee` `op.sompong` `op.kanya` (Operator), `tech.anan` `tech.wichai` `tech.preecha` (Technician), `sup.somchai` `sup.nok` (Supervisor)

### API หลัก

| เมธอด | ที่อยู่ | สิทธิ์ |
|---|---|---|
| GET | `/api/work-requests?status=&priority=&line=&equipmentCode=&page=&pageSize=` | ทุกคน |
| GET | `/api/work-requests/{id}` | ทุกคน |
| POST | `/api/work-requests` | Operator, Technician, Supervisor |
| POST | `/api/work-requests/{id}/assign` | Technician, Supervisor |
| POST | `/api/work-requests/{id}/close` | Technician, Supervisor |
| GET | `/api/equipment?line=` | ทุกคน |
| GET | `/api/reports/summary?from=yyyy-MM-dd&to=yyyy-MM-dd` | ทุกคน |

ข้อผิดพลาดตอบเป็น problem details (`application/problem+json`)

## รันเทสต์

```
dotnet test
```

เลือกรันบางกลุ่มด้วย trait

```
dotnet test --filter Area=Sla
dotnet test --filter Area=WorkRequest
```

แผนที่ว่าไฟล์ไหนเกี่ยวกับเทสต์กลุ่มไหนอยู่ที่ `tests/test-map.json` ค่า `*` หมายถึงรันทั้งหมด ค่า `e2e` หมายถึงรัน Playwright

### เทสต์ E2E (Playwright)

```
cd e2e
npm install
npx playwright install chromium
npm test
cd ..
```

บรรทัด `npx playwright install chromium` ทำครั้งแรกครั้งเดียว ถ้าเครื่องมี browser ของ Playwright 1.60 อยู่แล้วข้ามได้
เทสต์จะเปิด API เองที่พอร์ต 5080 ด้วย environment `E2E` และใช้ฐานข้อมูลแยก (`opstrack-e2e.db` ล้างใหม่ทุกครั้ง) จึงต้องปิด API ที่เปิดค้างไว้ก่อน

## MCP จำลอง Azure DevOps

อยู่ที่ `tools/ado-mock-mcp/` วิธีติดตั้งและต่อกับ Claude Code ดูใน `tools/ado-mock-mcp/README.md`

## แผนที่โฟลเดอร์

```
src/
  OpsTrack.Data/        ชั้นข้อมูล: entity, OpsTrackDbContext, ข้อมูลตัวอย่าง (Seed/)
  OpsTrack.Business/    ชั้น business: WorkRequestService, RunningNumberService, SlaCalculator,
                        ShiftCalendar, Guard, IClock, NotificationClient, ReportService
  OpsTrack.Api/         Web API + สิทธิ์แบบแลบ (Security/) + หน้าเว็บ (wwwroot/)
tests/
  OpsTrack.Business.Tests/   unit test (xUnit)
  OpsTrack.Api.Tests/        integration test (WebApplicationFactory + SQLite ในหน่วยความจำ)
  test-map.json              ไฟล์กับกลุ่มเทสต์ที่เกี่ยวข้อง
e2e/                    Playwright (TypeScript)
db/scripts/released/    สคริปต์ฐานข้อมูลที่ออกใช้งานแล้ว ห้ามแก้
db/scripts/pending/     สคริปต์ใหม่ที่ยังไม่ออกใช้งาน
docs/requirements/      คำขอจากผู้ใช้
docs/standards/         มาตรฐานการเขียนโค้ดและ security checklist
docs/sla-policy.md      นโยบาย SLA
docs/logs/              หลักฐานจากเครื่องจริงของ bug รายงานว่าง
ci/                     ตัวอย่าง pipeline รีวิว PR ด้วย Claude Code (ไม่ได้รันจริง)
tools/ado-mock-mcp/     MCP จำลอง Azure DevOps
```

## ข้อควรรู้

- repo นี้ยังไม่มี `CLAUDE.md` และโฟลเดอร์ `.claude/` ผู้เรียนจะสร้างเองในแลบ
- `db/scripts/` เป็นสคริปต์ SQL Server ตัวอย่าง ระบบที่รันในแลบใช้ SQLite และสร้างตารางจาก EF Core
- branch `lab/pr-review` ใช้ในแลบรีวิว PR อย่า merge เข้า `main`
