-- Выполните скрипт в базе EmployeeTrainingDB (или вашей БД из connection string).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CarParts' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.CarParts (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CarParts PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL
    );

    INSERT INTO dbo.CarParts (Name) VALUES
        (N'Двигатель'),
        (N'Ходовая часть'),
        (N'Кузов');
END
GO

IF COL_LENGTH('dbo.Articles', 'CarPartId') IS NULL
BEGIN
    ALTER TABLE dbo.Articles ADD CarPartId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Articles_CarParts')
BEGIN
    ALTER TABLE dbo.Articles
        ADD CONSTRAINT FK_Articles_CarParts FOREIGN KEY (CarPartId)
        REFERENCES dbo.CarParts (Id);
END
GO
