---
type: gotcha
date: 2026-11-19
tags: [dotnet, windows, test, example]
source: CLAUDE.md ของ repo หลักสูตร (หัวข้อแลบ OpsTrack)
---
# dotnet test ล้มแบบสุ่มด้วย 0x800711C7 บนเครื่องที่เปิด Smart App Control

## อาการ
`dotnet test` ล้มบางรอบ ข้อความมีรหัส `0x800711C7` ทั้งที่โค้ดไม่ได้เปลี่ยน

## สาเหตุ
Smart App Control ของ Windows 11 บล็อกไฟล์บางตัวที่ไม่มีลายเซ็น

## วิธีแก้
1. รันคำสั่งเดิมซ้ำ
2. ถ้ายังล้ม ให้ใช้เครื่องอื่นรันเทสต์
3. ห้ามปิด Smart App Control เพราะปิดแล้วเปิดกลับไม่ได้

## รู้ได้ยังไงว่าแก้แล้ว
รัน `dotnet test` ซ้ำแล้วผลเขียวตรงกับรอบก่อน

## เกี่ยวข้อง
- [[index]]
