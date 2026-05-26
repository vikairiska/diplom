-- Добавление специализации и города к таблице Users (выполнить один раз на существующей БД).
IF COL_LENGTH('dbo.Users', 'ProfessionalRole') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD ProfessionalRole NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.Users', 'City') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD City NVARCHAR(100) NULL;
END
GO
