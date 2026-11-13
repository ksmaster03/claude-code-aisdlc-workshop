-- V002__seed_lines.sql
-- Released with OpsTrack 1.0 on 2026-03-02. DO NOT EDIT: this script has already run on production.
-- Master data: production lines and equipment. Illustrative SQL Server script for the lab.

INSERT INTO dbo.Lines (Code, Name) VALUES
    (N'SMT-1',  N'ไลน์ SMT 1'),
    (N'SMT-2',  N'ไลน์ SMT 2'),
    (N'FATP-1', N'ไลน์ประกอบและทดสอบ FATP 1');

INSERT INTO dbo.Equipment (Code, Name, LineId)
SELECT v.Code, v.Name, l.Id
FROM (VALUES
    (N'SMT1-PRN-01',  N'เครื่องพิมพ์ครีมบัดกรี (Screen printer)',           N'SMT-1'),
    (N'SMT1-PNP-01',  N'เครื่องวางชิ้นส่วน (Pick and place)',               N'SMT-1'),
    (N'SMT1-RF-01',   N'เตาอบรีโฟลว์ (Reflow oven)',                       N'SMT-1'),
    (N'SMT2-PRN-01',  N'เครื่องพิมพ์ครีมบัดกรี (Screen printer)',           N'SMT-2'),
    (N'SMT2-PNP-01',  N'เครื่องวางชิ้นส่วน ตัวที่ 1 (Pick and place)',      N'SMT-2'),
    (N'SMT2-PNP-02',  N'เครื่องวางชิ้นส่วน ตัวที่ 2 (Pick and place)',      N'SMT-2'),
    (N'SMT2-RF-01',   N'เตาอบรีโฟลว์ (Reflow oven)',                       N'SMT-2'),
    (N'SMT2-AOI-01',  N'เครื่องตรวจชิ้นงานด้วยกล้อง (AOI)',                N'SMT-2'),
    (N'FATP1-ICT-01', N'เครื่องทดสอบวงจร (ICT)',                           N'FATP-1'),
    (N'FATP1-FCT-01', N'เครื่องทดสอบการทำงาน (FCT)',                       N'FATP-1'),
    (N'FATP1-LBL-01', N'เครื่องพิมพ์ฉลาก (Label printer)',                  N'FATP-1')
) AS v (Code, Name, LineCode)
JOIN dbo.Lines AS l ON l.Code = v.LineCode;
