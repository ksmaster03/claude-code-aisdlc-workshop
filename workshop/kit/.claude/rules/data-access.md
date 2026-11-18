---
paths:
  - "src/OpsTrack.Data/**/*.cs"
  - "src/OpsTrack.Business/**/*.cs"
---

# กติกา data access (EF Core)

- อ่านอย่างเดียวใช้ `AsNoTracking()`
- ห้าม `FromSqlRaw` กับ string interpolation ถ้าจำเป็นต้องใช้ SQL ให้ใช้ `FromSql($"...")` ที่ทำ parameter ให้
- การเขียนที่ต้องเช็คสถานะเดิม (เช่น ปิดงานที่ยังไม่ปิด) ใช้คำสั่งเดียว:
  `ExecuteUpdateAsync` พร้อมเงื่อนไขใน `Where` แล้วเช็คจำนวนแถวที่เปลี่ยน ถ้าเป็น 0 ให้ fail fast
- เลขที่เอกสารห้ามคำนวณจาก `MAX(...) + 1` ใช้ตัวนับที่เพิ่มแบบ atomic และมี unique index กันชนอีกชั้น
- `FirstOrDefault` ที่ผลอาจเป็น null ต้องเช็คด้วย `Guard` หรือคืนผลที่บอกว่า "ไม่พบ" อย่างชัดเจน
- รายการที่ส่งออก API ต้องมี paging และเพดานแถว (ตาม docs/standards/coding-standard.md)
- ห้ามแก้ไฟล์ใน `db/scripts/released/` ถ้าเปลี่ยน schema ให้เพิ่มไฟล์ใหม่ใน `db/scripts/pending/`
