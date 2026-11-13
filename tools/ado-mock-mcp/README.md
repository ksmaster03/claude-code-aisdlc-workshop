# ado-mock: MCP จำลอง Azure DevOps

MCP server ตัวเล็กสำหรับแลบ L9 ทำหน้าที่แทน Azure DevOps ของจริง มี work item 3 ชิ้น สั่งรันเทสต์ของ OpsTrack ได้ และบันทึกผลกลับเข้า work item ได้
ทำงานบนเครื่องอย่างเดียวผ่าน stdio ไม่ต่ออินเทอร์เน็ต

ของจริงที่ใช้หลังอบรมคือ Azure DevOps MCP ของ Microsoft (`@azure-devops/mcp`) ชื่อเครื่องมือและข้อมูลจะต่างจากตัวจำลองนี้ แต่หลักการสิทธิ์เหมือนกัน

## ติดตั้ง

ต้องมี Node.js 20 ขึ้นไป รันจาก root ของ repo

```
cd tools/ado-mock-mcp
npm install
cd ../..
```

ตรวจว่าใช้ได้

```
node tools/ado-mock-mcp/selftest.mjs
```

ต้องเห็น `SELFTEST PASSED` (selftest จะสั่ง `dotnet test` ของ unit test หนึ่งรอบ)

## ต่อกับ Claude Code

แบบเฉพาะเครื่องตัวเอง (scope local) รันที่ root ของ repo

```
claude mcp add --transport stdio ado-mock -- node tools/ado-mock-mcp/server.mjs
```

แบบแชร์ทั้งทีม (scope project) สร้างไฟล์ `.mcp.json` ที่ root ของ repo แล้ว commit

```json
{
  "mcpServers": {
    "ado-mock": {
      "type": "stdio",
      "command": "node",
      "args": ["tools/ado-mock-mcp/server.mjs"]
    }
  }
}
```

ตรวจสถานะด้วย `/mcp` ใน Claude Code หรือ `claude mcp list`

## สิทธิ์ที่แนะนำ

อ่านอนุญาตได้เลย เขียนต้องถามทุกครั้ง ใส่ใน `.claude/settings.json`

```json
{
  "permissions": {
    "allow": [
      "mcp__ado-mock__list_work_items",
      "mcp__ado-mock__get_work_item",
      "mcp__ado-mock__run_tests"
    ],
    "ask": [
      "mcp__ado-mock__add_test_cases",
      "mcp__ado-mock__update_test_result"
    ]
  }
}
```

`run_tests` ไม่ได้แก้ข้อมูลใน work item แต่สั่งรันโปรแกรมบนเครื่อง ถ้าทีมอยากคุมเข้มให้ย้ายไปไว้ใน `ask`

## เครื่องมือ

| เครื่องมือ | อ่านหรือเขียน | ใช้ทำอะไร |
|---|---|---|
| `list_work_items(state?)` | อ่าน | รายการ work item กรองตามสถานะได้ |
| `get_work_item(id)` | อ่าน | รายละเอียดเต็ม: คำอธิบาย AC ขั้นตอนทำซ้ำ ลิงก์ test case ประวัติ |
| `add_test_cases(workItemId, cases)` | เขียน | เพิ่ม test case แต่ละข้อมี `title` `steps` `expected` |
| `run_tests(kind, filter?)` | รันโปรแกรม | `unit` = `tests/OpsTrack.Business.Tests`, `api` = `tests/OpsTrack.Api.Tests` (filter แบบ `dotnet test --filter` เช่น `Area=Sla`), `e2e` = Playwright ใน `e2e/` (filter คือ `--grep`) ส่งกลับจำนวนผ่าน ไม่ผ่าน และชื่อเทสต์ที่ไม่ผ่าน |
| `update_test_result(workItemId, outcome, summaryMarkdown)` | เขียน | บันทึกผล (`Passed` `Failed` `Blocked` `NotExecuted`) เป็นไฟล์ Markdown และเพิ่มในประวัติของ work item |

## work item ที่มีให้

| id | ชนิด | เรื่อง |
|---|---|---|
| 101 | User Story | ปิดงานซ่อมพร้อมบันทึกสาเหตุ (REQ-101) ยังไม่มี AC |
| 102 | Task | เทสต์อัตโนมัติสำหรับการปิดงานซ่อม |
| 103 | Bug | รายงานสรุปว่างเปล่าบนเครื่องจริง |

## ข้อมูลอยู่ที่ไหน

- `data/workitems.json` ข้อมูลตั้งต้น ไม่ถูกแก้
- `data/workitems.local.json` สิ่งที่เครื่องมือเขียนจะอยู่ไฟล์นี้ (ไม่เข้า git) ลบทิ้งเพื่อเริ่มใหม่
- `data/results/<id>-<เวลา>.md` สรุปผลเทสต์ที่บันทึกด้วย `update_test_result` (ไม่เข้า git)
- `.runs/` ไฟล์ผล `.trx` ของการรันเทสต์ (ไม่เข้า git)

## ตั้งค่าเพิ่ม (ไม่บังคับ)

| ตัวแปรแวดล้อม | ค่าเริ่มต้น | ใช้ทำอะไร |
|---|---|---|
| `OPSTRACK_ROOT` | สองชั้นเหนือโฟลเดอร์นี้ | root ของ repo ที่จะรันเทสต์ |
| `ADO_MOCK_TEST_TIMEOUT_MS` | `300000` | เวลาสูงสุดของการรันเทสต์หนึ่งครั้ง เกินแล้วหยุดทั้งชุด |
