---
name: pr-review
description: รีวิว diff ของ branch ปัจจุบันเทียบ main แบบรอบเดียวจบ ตามมาตรฐานทีมและ checklist ความปลอดภัย ออกรายงาน JSON และ Markdown ใช้เมื่อผู้ใช้ขอรีวิว PR รีวิว branch หรือก่อน merge
allowed-tools: Bash(git diff *), Bash(git merge-base *), Bash(git log *), Read, Grep, Glob
---

# PR review รอบเดียวจบ

## อ่านก่อนเริ่ม
- `docs/standards/coding-standard.md`
- `docs/standards/security-checklist.md`
- diff: `git diff $(git merge-base HEAD main)...HEAD`
- ไฟล์รอบข้างที่ diff อ้างถึง (อ่านให้พอเข้าใจ ไม่ต้องอ่านทั้ง repo)

## ไล่ให้ครบทุกหมวดก่อนตอบ
1. ความถูกต้องของ logic และค่าขอบ
2. ความปลอดภัย: injection, สิทธิ์, ความลับในโค้ด, การตรวจ input, CSV/formula injection
3. ข้อมูลและฐานข้อมูล: N+1, query ไม่มีเพดาน, race condition, transaction
4. การจัดการ error: กลืน exception, ข้อความ error รั่วข้อมูล
5. มาตรฐานทีม: การแบ่งชั้น, ใช้ของเดิม, การตั้งชื่อ
6. เทสต์: AC ที่ยังไม่มีเทสต์, เทสต์ที่ถูกแก้ให้ผ่าน

## กติกา
- รอบเดียวจบ ห้ามถามกลับ ถ้าข้อมูลไม่พอให้บอกในหัวข้อ "สิ่งที่ไม่ได้ตรวจ"
- ทุกข้อต้องมีหลักฐานจาก diff (ไฟล์และบรรทัด) ไม่มีหลักฐานห้ามรายงาน
- ไม่รายงานเรื่องรสนิยม (จัดรูปแบบ, ชื่อที่ชอบมากกว่า) ยกเว้นขัดมาตรฐานที่เขียนไว้

## รูปแบบผลลัพธ์
1. JSON array ใน code block:
```json
[{"severity":"high|medium|low","category":"security|correctness|data|error-handling|standard|test",
  "file":"path","line":0,"evidence":"...","fix":"...","confidence":"high|medium|low"}]
```
2. สรุป Markdown: จำนวนตามระดับ, 3 เรื่องที่ต้องแก้ก่อน merge, สิ่งที่ไม่ได้ตรวจ
