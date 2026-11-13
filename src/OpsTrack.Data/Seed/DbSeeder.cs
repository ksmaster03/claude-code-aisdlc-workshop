using System.Globalization;
using OpsTrack.Data.Entities;

namespace OpsTrack.Data.Seed;

/// <summary>
/// Demo data for the lab. Safe to call on every start: it does nothing when lines already exist.
/// </summary>
public static class DbSeeder
{
    public static void Seed(OpsTrackDbContext db)
    {
        if (db.Lines.Any())
        {
            return;
        }

        using var tx = db.Database.BeginTransaction();

        var smt1 = new Line { Code = "SMT-1", Name = "ไลน์ SMT 1" };
        var smt2 = new Line { Code = "SMT-2", Name = "ไลน์ SMT 2" };
        var fatp1 = new Line { Code = "FATP-1", Name = "ไลน์ประกอบและทดสอบ FATP 1" };
        db.Lines.AddRange(smt1, smt2, fatp1);

        var equipment = new List<Equipment>
        {
            new() { Code = "SMT1-PRN-01", Name = "เครื่องพิมพ์ครีมบัดกรี (Screen printer)", Line = smt1 },
            new() { Code = "SMT1-PNP-01", Name = "เครื่องวางชิ้นส่วน (Pick and place)", Line = smt1 },
            new() { Code = "SMT1-RF-01", Name = "เตาอบรีโฟลว์ (Reflow oven)", Line = smt1 },
            new() { Code = "SMT2-PRN-01", Name = "เครื่องพิมพ์ครีมบัดกรี (Screen printer)", Line = smt2 },
            new() { Code = "SMT2-PNP-01", Name = "เครื่องวางชิ้นส่วน ตัวที่ 1 (Pick and place)", Line = smt2 },
            new() { Code = "SMT2-PNP-02", Name = "เครื่องวางชิ้นส่วน ตัวที่ 2 (Pick and place)", Line = smt2 },
            new() { Code = "SMT2-RF-01", Name = "เตาอบรีโฟลว์ (Reflow oven)", Line = smt2 },
            new() { Code = "SMT2-AOI-01", Name = "เครื่องตรวจชิ้นงานด้วยกล้อง (AOI)", Line = smt2 },
            new() { Code = "FATP1-ICT-01", Name = "เครื่องทดสอบวงจร (ICT)", Line = fatp1 },
            new() { Code = "FATP1-FCT-01", Name = "เครื่องทดสอบการทำงาน (FCT)", Line = fatp1 },
            new() { Code = "FATP1-LBL-01", Name = "เครื่องพิมพ์ฉลาก (Label printer)", Line = fatp1 },
        };
        db.Equipment.AddRange(equipment);
        db.SaveChanges();

        var byCode = equipment.ToDictionary(e => e.Code);
        var counters = new Dictionary<string, int>();
        var requests = new List<WorkRequest>();

        foreach (var s in SeedRows)
        {
            var created = Parse(s.CreatedAt);
            var prefix = "WR-" + created.ToString("yyMM", CultureInfo.InvariantCulture);
            counters[prefix] = counters.TryGetValue(prefix, out var n) ? n + 1 : 1;

            requests.Add(new WorkRequest
            {
                Number = prefix + "-" + counters[prefix].ToString("0000", CultureInfo.InvariantCulture),
                EquipmentId = byCode[s.EquipmentCode].Id,
                Title = s.Title,
                Description = s.Description,
                Priority = s.Priority,
                Status = s.Status,
                ReportedBy = s.ReportedBy,
                AssignedTo = s.AssignedTo,
                CreatedAt = created,
                ClosedAt = s.ClosedAt == null ? null : Parse(s.ClosedAt),
                ClosedBy = s.ClosedBy,
            });
        }

        db.WorkRequests.AddRange(requests);
        db.SaveChanges();

        foreach (var c in counters)
        {
            db.RunningNumbers.Add(new RunningNumber { Key = c.Key, Value = c.Value });
        }

        foreach (var wr in requests)
        {
            db.AuditLogs.Add(new AuditLog
            {
                Entity = "WorkRequest",
                EntityId = wr.Number,
                Action = "Create",
                By = wr.ReportedBy,
                At = wr.CreatedAt,
                Detail = "Priority=" + wr.Priority,
            });

            if (wr.AssignedTo != null)
            {
                db.AuditLogs.Add(new AuditLog
                {
                    Entity = "WorkRequest",
                    EntityId = wr.Number,
                    Action = "Assign",
                    By = wr.AssignedTo,
                    At = wr.CreatedAt.AddMinutes(20),
                    Detail = "AssignedTo=" + wr.AssignedTo,
                });
            }

            if (wr.ClosedAt != null)
            {
                db.AuditLogs.Add(new AuditLog
                {
                    Entity = "WorkRequest",
                    EntityId = wr.Number,
                    Action = "Close",
                    By = wr.ClosedBy,
                    At = wr.ClosedAt.Value,
                    Detail = null,
                });
            }
        }

        db.SaveChanges();
        tx.Commit();
    }

    private static DateTime Parse(string value) =>
        DateTime.ParseExact(value, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private sealed record Row(
        string CreatedAt,
        string EquipmentCode,
        string Title,
        string Description,
        Priority Priority,
        WorkRequestStatus Status,
        string ReportedBy,
        string AssignedTo,
        string ClosedAt,
        string ClosedBy);

    private const WorkRequestStatus Open = WorkRequestStatus.Open;
    private const WorkRequestStatus InProgress = WorkRequestStatus.InProgress;
    private const WorkRequestStatus Closed = WorkRequestStatus.Closed;

    private static readonly Row[] SeedRows =
    {
        new("2026-10-20 08:15", "SMT2-RF-01", "เตารีโฟลว์อุณหภูมิโซน 5 แกว่งเกินสเปก",
            "อุณหภูมิโซน 5 ขึ้นลงระหว่าง 245-262 องศา, เกินสเปก ±5 องศา\nกะดึกปรับ \"profile B\" แล้วยังไม่หาย",
            Priority.P1, Closed, "op.malee", "tech.anan", "2026-10-20 11:40", "tech.anan"),
        new("2026-10-21 14:30", "SMT1-PNP-01", "หัวดูดเบอร์ 3 ดูดชิ้นงานไม่ติด",
            "ชิ้น 0402 หลุดบ่อย นับได้ประมาณ 40 ชิ้นต่อชั่วโมง",
            Priority.P2, Closed, "op.sompong", "tech.wichai", "2026-10-22 09:10", "tech.wichai"),
        new("2026-10-23 02:20", "SMT2-AOI-01", "AOI แจ้ง false call สูงผิดปกติหลังเปลี่ยนโปรแกรม",
            "- false call ขึ้นจาก 2% เป็น 11%\n- เกิดหลังโหลดโปรแกรมรุ่น X200 rev C",
            Priority.P3, Closed, "op.kanya", "tech.preecha", "2026-10-24 16:00", "tech.anan"),
        new("2026-10-26 10:05", "FATP1-ICT-01", "pin ของ fixture ICT สึก วัดค่าไม่ผ่านหลายบอร์ด",
            "บอร์ดรุ่น X200 ไม่ผ่านที่จุดวัด TP14, TP15 ติดกัน 12 บอร์ด",
            Priority.P2, Closed, "op.malee", "tech.anan", "2026-10-28 13:30", "tech.anan"),
        new("2026-10-28 22:45", "SMT1-RF-01", "สายพานลำเลียงขาเข้าเตาสะดุดเป็นช่วง ๆ",
            "บอร์ดชนกันที่ทางเข้าเตา 3 ครั้งในกะดึก",
            Priority.P1, Closed, "op.sompong", "tech.wichai", "2026-10-29 06:10", "tech.preecha"),
        new("2026-10-29 09:30", "FATP1-LBL-01", "เครื่องพิมพ์ฉลากพิมพ์บาร์โค้ดจาง สแกนไม่ติด",
            "สแกนไม่ติดประมาณครึ่งหนึ่งของกล่อง",
            Priority.P3, Closed, "op.kanya", "tech.preecha", "2026-10-30 10:00", "tech.preecha"),
        new("2026-10-30 13:20", "SMT2-PRN-01", "เครื่องพิมพ์ครีมบัดกรีพิมพ์ไม่เต็มแพด",
            "พบที่ตำแหน่ง U7, U9 เป็น \"bridge\" สลับกับ \"insufficient\"",
            Priority.P2, Closed, "op.malee", "tech.anan", "2026-10-31 08:00", "sup.somchai"),
        new("2026-10-31 18:40", "SMT2-PNP-02", "feeder 8 มม. ช่อง 12 ป้อนเทปไม่เดิน",
            "เปลี่ยน feeder สำรองแล้วยังเป็น รอสั่งอะไหล่",
            Priority.P2, InProgress, "op.sompong", "tech.wichai", null, null),
        new("2026-11-01 07:50", "SMT2-RF-01", "ไนโตรเจนในเตาแรงดันตกช่วงเช้า",
            "แรงดันตกจาก 6 บาร์เหลือ 4.2 บาร์ ช่วง 07:30-08:00",
            Priority.P1, Closed, "op.malee", "tech.anan", "2026-11-01 10:30", "tech.anan"),
        new("2026-11-02 03:15", "SMT1-PRN-01", "กล้อง fiducial อ่านเครื่องหมายบนบอร์ดไม่เจอ",
            "เป็นเฉพาะบอร์ดล็อตใหม่จากซัพพลายเออร์ B",
            Priority.P2, Closed, "op.kanya", "tech.preecha", "2026-11-02 15:00", "tech.preecha"),
        new("2026-11-03 11:00", "FATP1-FCT-01", "FCT ค้างที่ขั้นทดสอบ USB บ่อย",
            "ค้างประมาณ 1 ใน 20 บอร์ด ต้องรีสตาร์ตโปรแกรม",
            Priority.P2, Closed, "op.sompong", "tech.wichai", "2026-11-05 09:00", "tech.wichai"),
        new("2026-11-04 08:30", "SMT2-AOI-01", "ไฟส่องสว่าง AOI ด้านซ้ายกะพริบ",
            "กะพริบทุก 2-3 วินาที ภาพตรวจมืดเป็นช่วง",
            Priority.P3, Closed, "op.malee", "tech.preecha", "2026-11-05 14:00", "tech.anan"),
        new("2026-11-05 16:45", "SMT2-PNP-01", "แขนกลแจ้ง error E-203 servo overload",
            "เครื่องหยุดทั้งไลน์ ต้องรีเซ็ตทุก 10 นาที",
            Priority.P1, Closed, "op.kanya", "tech.anan", "2026-11-05 19:30", "tech.anan"),
        new("2026-11-06 01:10", "SMT1-RF-01", "พัดลมระบายความร้อนโซน cooling เสียงดัง",
            "เสียงดังผิดปกติ ยังไม่กระทบคุณภาพงาน",
            Priority.P3, InProgress, "op.sompong", "tech.wichai", null, null),
        new("2026-11-06 10:20", "FATP1-ICT-01", "ICT รายงาน open circuit ผิดพลาดที่ connector J4",
            "@ทีมวิศวกรรมทดสอบ ช่วยดูโปรแกรมด้วย บอร์ดที่ fail ส่งไปตรวจซ้ำผ่านทุกตัว",
            Priority.P2, InProgress, "op.malee", "tech.preecha", null, null),
        new("2026-11-07 09:00", "SMT2-PRN-01", "ใบปาด squeegee บิ่น ต้องเปลี่ยน",
            "บิ่นประมาณ 2 มม. ที่ขอบด้านขวา",
            Priority.P2, Closed, "op.sompong", "tech.anan", "2026-11-07 15:20", "tech.anan"),
        new("2026-11-08 14:10", "SMT2-RF-01", "ค่าอุณหภูมิบนจอไม่ตรงกับเครื่องวัด profile",
            "ต่างกันประมาณ 6-8 องศา, วัดด้วยเครื่องวัด profile เมื่อ 14:00",
            Priority.P2, InProgress, "op.kanya", "tech.anan", null, null),
        new("2026-11-09 06:40", "SMT1-PNP-01", "ประตูเซฟตี้ล็อกไม่ติด เครื่องไม่ยอมรัน",
            "เซนเซอร์ประตูด้านหน้าไม่ส่งสัญญาณ",
            Priority.P1, Closed, "op.malee", "tech.wichai", "2026-11-09 08:05", "tech.wichai"),
        new("2026-11-09 20:30", "FATP1-LBL-01", "ริบบอนเครื่องพิมพ์ฉลากยับ",
            "ฉลากมีรอยขาวเป็นเส้น",
            Priority.P3, Open, "op.kanya", null, null, null),
        new("2026-11-10 08:05", "SMT2-PNP-02", "nozzle changer หยิบหัวผิดตำแหน่ง",
            "เกิด 2 ครั้งในกะเช้า เครื่องหยุดเอง",
            Priority.P2, InProgress, "op.sompong", "tech.preecha", null, null),
        new("2026-11-10 13:30", "SMT2-AOI-01", "AOI ไม่บันทึกภาพ NG ลงเซิร์ฟเวอร์",
            "แชร์ไดรฟ์ \\\\qa-share\\aoi เต็มหรือเปล่า ยังไม่ได้ตรวจ",
            Priority.P3, Open, "op.malee", null, null, null),
        new("2026-11-11 02:50", "SMT2-RF-01", "เตาแจ้งเตือน O2 ppm สูงเกิน 1000",
            "ค่าปกติไม่เกิน 500 ppm",
            Priority.P1, InProgress, "op.kanya", "tech.anan", null, null),
        new("2026-11-11 10:15", "FATP1-FCT-01", "จิ๊กยึดบอร์ด FCT หลวม บอร์ดขยับระหว่างทดสอบ",
            "สกรูยึดจิ๊กด้านซ้ายหาย 1 ตัว",
            Priority.P2, Open, "op.sompong", null, null, null),
        new("2026-11-11 15:40", "SMT1-PRN-01", "เครื่องพิมพ์แจ้งครีมบัดกรีหมดทั้งที่ยังมี",
            "เซนเซอร์วัดระดับครีมน่าจะสกปรก",
            Priority.P3, Open, "op.malee", null, null, null),
        new("2026-11-12 07:20", "SMT2-PNP-01", "feeder 12 มม. ช่อง 3 ฟ้อง missing part",
            "มีชิ้นงานในเทปครบ แต่เครื่องฟ้องว่าไม่มี",
            Priority.P2, Open, "op.kanya", null, null, null),
        new("2026-11-12 09:45", "SMT2-PRN-01", "แรงดันลมเข้าเครื่องต่ำกว่า 5 บาร์",
            "-0.5 บาร์จากค่าปกติ, ช่วงที่เครื่องอัดลมตัวที่ 2 ตัด",
            Priority.P1, Open, "op.sompong", null, null, null),
        new("2026-11-12 11:30", "FATP1-ICT-01", "คอมพิวเตอร์คุมเครื่อง ICT ช้ามาก",
            "เปิดโปรแกรมทดสอบใช้เวลาเกือบ 5 นาที",
            Priority.P3, Open, "op.malee", null, null, null),
        new("2026-11-12 14:00", "SMT1-RF-01", "ขอเปลี่ยนสายพาน mesh ตามแผน PM",
            "ตามแผนบำรุงรักษาประจำไตรมาส",
            Priority.P3, Open, "sup.nok", null, null, null),
        new("2026-11-12 18:25", "SMT2-AOI-01", "AOI สแกนบาร์โค้ดบอร์ดไม่ได้หลังย้ายตำแหน่งกล้อง",
            "=> เช็ดเลนส์แล้ว ยังอ่านไม่ได้\n=> สงสัยระยะโฟกัส",
            Priority.P2, Open, "op.kanya", null, null, null),
        new("2026-11-12 22:10", "SMT2-RF-01", "เตารีโฟลว์หยุดกลางคัน แจ้ง heater zone 3 fault",
            "บอร์ดค้างในเตา 6 แผ่น ต้องนำออกด้วยมือ",
            Priority.P1, Open, "op.malee", null, null, null),
    };
}
