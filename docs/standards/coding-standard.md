# มาตรฐานการเขียนโค้ด OpsTrack

ใช้กับทุก PR ผู้รีวิวอ้างข้อในเอกสารนี้ได้ตรง ๆ เช่น "CS-5.3"

## CS-1 การแบ่งชั้น

1. `OpsTrack.Data` มีแค่ entity, `OpsTrackDbContext` และ seed
2. `OpsTrack.Business` มี business rule ทั้งหมด (service, การคำนวณ, การตรวจ input)
3. `OpsTrack.Api` มี controller, การตรวจสิทธิ์ และหน้าเว็บใน `wwwroot`
4. controller ห้ามใช้ `OpsTrackDbContext` ตรง ๆ และห้ามมี business rule ให้เรียก service ใน Business เท่านั้น

## CS-2 async

1. เมธอดที่แตะฐานข้อมูลหรือ HTTP เป็น async ทั้งเส้น ลงท้ายชื่อด้วย `Async`
2. ห้ามใช้ `.Result` `.Wait()` `GetAwaiter().GetResult()`
3. รับและส่งต่อ `CancellationToken` ทุกชั้น

## CS-3 ใช้ของที่มีอยู่แล้ว ห้ามสร้างซ้ำ

ก่อนเขียน helper ใหม่ ให้ค้นในโค้ดก่อน ของที่ต้องใช้มีดังนี้

| เรื่อง | ใช้ตัวนี้ |
|---|---|
| ตรวจ input | `Guard` (`NotNull`, `NotEmpty`, `Positive`) ใน `OpsTrack.Business.Common` |
| เวลาปัจจุบัน | `IClock` ห้ามเรียก `DateTime.Now` ใน Business |
| SLA กำหนดเสร็จ เกิน SLA หรือไม่ | `SlaCalculator` |
| วันผลิตและกะ | `ShiftCalendar` |
| เลขที่เอกสาร | `RunningNumberService` |
| แจ้งเตือนออกไปภายนอก | `INotificationClient` |

ถ้าของเดิมไม่พอ ให้เพิ่มเมธอดในคลาสเดิม อย่าสร้างคลาสใหม่ที่ทำเรื่องเดียวกัน

## CS-4 การตรวจ input และข้อผิดพลาด

1. ตรวจ input ที่ขอบของ service ด้วย `Guard` ถ้าค้นข้อมูลอ้างอิงไม่เจอ (เช่นรหัสเครื่องจักร) ต้องตอบ 400 พร้อมข้อความที่ผู้ใช้อ่านเข้าใจ ห้ามปล่อยให้เป็น 500
2. โยน `ValidationFailedException` (400) `NotFoundException` (404) `ConflictException` (409) แล้วให้ `OpsTrackExceptionHandler` แปลงเป็น problem details
3. ห้าม `catch (Exception)` แล้วกลืน ห้าม catch แล้วตอบ 200 พร้อมข้อมูลว่าง

## CS-5 EF Core และฐานข้อมูล

1. ห้ามใช้ `FromSqlRaw` หรือ `ExecuteSqlRaw` กับ string ที่ต่อหรือ interpolate เอง ให้ใช้ LINQ หรือ `FromSql` / `FromSqlInterpolated` ที่ส่งเป็นพารามิเตอร์
2. query ที่อ่านอย่างเดียวใส่ `AsNoTracking()`
3. รายการที่ส่งผ่าน API ต้องแบ่งหน้า สูงสุด 500 แถวต่อหน้า
4. การส่งออกไฟล์ (export) สูงสุด 10,000 แถวต่อครั้ง ถ้าเกินให้ตอบ 400 และบอกให้เลือกช่วงวันที่ให้แคบลง
5. ห้าม query ในลูป (N+1) ให้ใช้ `Include` หรือ projection ครั้งเดียว
6. การเปลี่ยนสถานะที่มีเงื่อนไข เช่น ปิดงานได้เฉพาะงานที่ยังไม่ปิด ให้ใช้คำสั่งเดียวแบบมีเงื่อนไข (`ExecuteUpdateAsync` ที่มีเงื่อนไขใน WHERE แล้วดูจำนวนแถว) ห้ามอ่านขึ้นมาเช็คแล้วค่อยเขียน
7. ค่าที่ห้ามซ้ำ (เช่นเลขที่งาน) ต้องมี unique index ที่ฐานข้อมูลด้วย ไม่ใช่เช็คในโค้ดอย่างเดียว

## CS-6 สคริปต์ฐานข้อมูล

1. ไฟล์ใน `db/scripts/released/` ออกใช้งานแล้ว ห้ามแก้ทุกกรณี
2. การเปลี่ยนแปลงใหม่ให้สร้างไฟล์ใน `db/scripts/pending/` ชื่อ `V<เลขถัดไป>__<สิ่งที่ทำ>.sql`

## CS-7 Logging

1. ใช้ `ILogger` แบบมีชื่อพารามิเตอร์ เช่น `LogInformation("Work request {Number} closed", number)` ห้ามต่อ string เอง
2. ห้าม log รหัสผ่าน token หรือข้อมูลส่วนบุคคลเกินชื่อผู้ใช้
3. `Diagnostics:LogSqlParameters` เปิดได้ชั่วคราวตอนตรวจปัญหาเท่านั้น แล้วต้องปิดกลับ

## CS-8 การตั้งชื่อ

1. คลาส เมธอด property เป็น PascalCase, field ส่วนตัวเป็น `_camelCase`
2. ชื่อเทสต์บอกพฤติกรรม เช่น `Closing_a_closed_request_returns_409`
3. คลาสเทสต์ทุกคลาสต้องมี `[Trait("Area", "...")]` ที่ตรงกับ `tests/test-map.json`

## CS-9 เทสต์

1. แก้บั๊กต้องมีเทสต์ที่แดงก่อนแก้และเขียวหลังแก้
2. การเรียกระบบภายนอก (เช่น webhook แจ้งเตือน) ในเทสต์ต้องใช้ stub เสมอ ดูตัวอย่าง `StubHttpMessageHandler`
3. ห้ามแก้เทสต์เพื่อให้ผ่าน ถ้าเทสต์ผิดจริงให้แยกเป็น commit ที่อธิบายเหตุผล
