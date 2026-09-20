-- ============================================================
-- 种子数据：初始化仪器类型(A/B/C)及若干仪器设备
-- 在 init.sql 执行完毕后运行
-- ============================================================

USE [AgilentQuiz];
GO

-- 仪器类型
IF NOT EXISTS (SELECT 1 FROM InstrumentTypes WHERE Code = 'A')
    INSERT INTO InstrumentTypes (Id, Name, Code, Description, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), '类型A-色谱仪', 'A', '高效液相色谱/气相色谱类仪器', 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM InstrumentTypes WHERE Code = 'B')
    INSERT INTO InstrumentTypes (Id, Name, Code, Description, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), '类型B-光谱仪', 'B', '紫外/红外/质谱类光谱仪器', 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM InstrumentTypes WHERE Code = 'C')
    INSERT INTO InstrumentTypes (Id, Name, Code, Description, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), '类型C-显微镜', 'C', '光学/电子显微镜类仪器', 1, GETUTCDATE(), GETUTCDATE());
GO

-- 仪器设备（每类型2台）
DECLARE @TypeIdA UNIQUEIDENTIFIER = (SELECT Id FROM InstrumentTypes WHERE Code = 'A');
DECLARE @TypeIdB UNIQUEIDENTIFIER = (SELECT Id FROM InstrumentTypes WHERE Code = 'B');
DECLARE @TypeIdC UNIQUEIDENTIFIER = (SELECT Id FROM InstrumentTypes WHERE Code = 'C');

IF NOT EXISTS (SELECT 1 FROM Instruments WHERE Code = 'A-001')
    INSERT INTO Instruments (Id, InstrumentTypeId, Name, Code, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @TypeIdA, 'HPLC-001', 'A-001', 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Instruments WHERE Code = 'A-002')
    INSERT INTO Instruments (Id, InstrumentTypeId, Name, Code, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @TypeIdA, 'GC-001', 'A-002', 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Instruments WHERE Code = 'B-001')
    INSERT INTO Instruments (Id, InstrumentTypeId, Name, Code, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @TypeIdB, 'UV-Vis-001', 'B-001', 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Instruments WHERE Code = 'B-002')
    INSERT INTO Instruments (Id, InstrumentTypeId, Name, Code, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @TypeIdB, 'MS-001', 'B-002', 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Instruments WHERE Code = 'C-001')
    INSERT INTO Instruments (Id, InstrumentTypeId, Name, Code, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @TypeIdC, 'SEM-001', 'C-001', 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Instruments WHERE Code = 'C-002')
    INSERT INTO Instruments (Id, InstrumentTypeId, Name, Code, Status, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @TypeIdC, 'TEM-001', 'C-002', 1, GETUTCDATE(), GETUTCDATE());
GO

PRINT 'Seed data inserted successfully.';
