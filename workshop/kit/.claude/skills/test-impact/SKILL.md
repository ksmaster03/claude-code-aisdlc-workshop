---
name: test-impact
description: เลือกรันเฉพาะเทสต์ที่เกี่ยวกับการเปลี่ยนแปลงใน branch ปัจจุบัน จาก git diff และแผนที่ tests/test-map.json แล้วรายงานเหตุผลรายไฟล์ ใช้เมื่อผู้ใช้ขอ "รันเทสต์ที่เกี่ยว" "ไม่ต้องรันทั้งหมด" หรือก่อนเปิด PR
allowed-tools: Bash(git diff *), Bash(git merge-base *), Bash(dotnet test *), Bash(npx playwright test *), Read, Grep, Glob
---

# เลือกรันเทสต์จาก diff

1. หาไฟล์ที่เปลี่ยนเทียบกับ main
   `git diff --name-only $(git merge-base HEAD main)`
2. อ่าน `tests/test-map.json` แล้วจับคู่ไฟล์ที่เปลี่ยนกับค่าในแผนที่ ค่ามี 4 แบบ
   - `Area=Xxx` → filter ของ dotnet test (เช่น `Area=Sla`)
   - `*` → ไฟล์กลาง ต้องรันทั้งหมด
   - `e2e` → รัน Playwright ใน `e2e/` (`npx playwright test`)
   - `project:<path>` → รันเฉพาะโปรเจกต์เทสต์นั้น (`dotnet test <path>`)
3. ไฟล์ที่ไม่มีในแผนที่ ให้ค้นด้วย Grep ว่าคลาสในไฟล์นั้นถูกเทสต์ไหนอ้างถึง แล้วเสนอให้เพิ่มลงแผนที่
4. ถ้าไฟล์ที่เปลี่ยนเป็นของกลาง (DbContext, Program.cs, Directory.Build.props, *.csproj) ให้รันทั้งหมด
5. รวม filter เป็นคำสั่งเดียว เช่น `dotnet test OpsTrack.sln --filter "Area=Sla|Area=WorkRequest"` แล้วรัน

## รายงาน
```
ไฟล์ที่เปลี่ยน -> เทสต์ที่เลือก -> เหตุผล
...
คำสั่งที่รัน: ...
ผล: ผ่าน N / ไม่ผ่าน M  (รายชื่อที่ไม่ผ่าน)
ไฟล์ที่ไม่มีในแผนที่: ... (เสนอบรรทัดที่ควรเพิ่มใน test-map.json)
ความเสี่ยงที่ไม่ได้รันทั้งหมด: ...
```

ถ้ามี Work Item ที่เกี่ยวข้อง (เลขใน branch หรือ commit) ให้ใส่เลขไว้ในรายงานด้วย
