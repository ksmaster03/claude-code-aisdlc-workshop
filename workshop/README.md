# ของที่ใช้ในเวิร์กช็อป AI-SDLC ด้วย Claude Code

repo นี้คือ **OpsTrack** ระบบแจ้งซ่อมจำลองที่ใช้ทำแลบทั้งสองวัน พร้อมชุดเครื่องมือที่แจกระหว่างเรียน
โค้ดและข้อมูลทั้งหมดเป็นของสมมติ สร้างขึ้นเพื่อฝึกอบรม มีจุดบกพร่องที่ตั้งใจฝังไว้ให้แลบพาไปหา **ห้ามนำไปใช้งานจริง**

## เริ่มใช้

```bash
git clone https://github.com/ksmaster03/claude-code-aisdlc-workshop.git opstrack
cd opstrack
dotnet build
dotnet test
```

รายละเอียดการเปิดระบบ ผู้ใช้จำลอง และโครงสร้างโค้ดอยู่ใน [README หลัก](../README.md)

| branch | ใช้ในแลบ |
|---|---|
| `main` | L1 ถึง L7, L9, L10 และแลบใหม่ของวันที่ 2 (L0, LM) |
| `lab/pr-review` | L8 รีวิว branch ที่ฝังบั๊กไว้ (`git switch lab/pr-review`) |

ค่า `ExportSigningKey` ใน branch `lab/pr-review` เป็นคีย์ปลอมที่ตั้งใจใส่ไว้ให้รีวิวจับ ไม่ใช่ความลับจริง

## ในโฟลเดอร์ `workshop/`

| ที่อยู่ | คืออะไร | ใช้ตอนไหน |
|---|---|---|
| `check-machine.ps1` | ตรวจเครื่องก่อนเรียน: Git, .NET, Node.js, Claude Code, VS Code และ extension, การต่อเน็ต, build และเทสต์ของ OpsTrack | ก่อนเรียน `powershell -ExecutionPolicy Bypass -File workshop\check-machine.ps1 -RepoPath .` |
| `kit/` | ชุดมาตรฐานทีมตัวอย่าง: `CLAUDE.md`, `.claude/settings.json`, hooks, rules, skills (ac-check, test-impact, pr-review), subagents (test-writer, req-checker), แม่แบบส่งต่องาน, `.mcp.json` | L3 คัดลอกเข้า repo แล้วปรับตามทีม |
| `memory/vault-template/` | แม่แบบ vault ความจำภายนอก (Obsidian): index, แม่แบบโน้ต 4 แบบ, โน้ตตัวอย่าง, ตัวอย่าง `CLAUDE.local.md` และ `settings.local.json` | LM คัดลอกไปไว้ใน Google Drive, OneDrive ของบริษัท หรือโฟลเดอร์ในเครื่อง |
| `memory/vector-demo/` | สาธิต vector DB ในเครื่อง (Chroma + โมเดล multilingual รองรับไทย) ต่อเข้า Claude Code เป็น MCP server แบบอ่านอย่างเดียว | ผู้สอนสาธิต วิธีรันและผลจริงอยู่ใน README ของโฟลเดอร์ |

วิธีลงชุดมาตรฐานทีมใน repo ของตัวเอง (รันจากโฟลเดอร์ repo ปลายทาง):

```bash
cp -r path/to/opstrack/workshop/kit/.claude .
cp path/to/opstrack/workshop/kit/CLAUDE.md path/to/opstrack/workshop/kit/.mcp.json .
mkdir -p docs/handoff && cp path/to/opstrack/workshop/kit/docs/handoff/TEMPLATE.md docs/handoff/
```

แก้ `CLAUDE.md` และ `.claude/protected-paths.txt` ให้ตรงกับ repo ของทีมก่อนใช้จริง

---

จัดทำโดย GrowGenius · © 2026 ใช้เพื่อการเรียนรู้
