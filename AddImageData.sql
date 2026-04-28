-- Скрипт для добавления поля ImageData в таблицу Articles
-- Выполните этот скрипт в SQL Server Management Studio

USE [YourDatabaseName]; -- Замените на имя вашей базы данных
GO

IF NOT EXISTS (
    SELECT * 
    FROM sys.columns 
    WHERE name = N'ImageData' 
    AND object_id = OBJECT_ID(N'[dbo].[Articles]')
)
BEGIN
    ALTER TABLE [dbo].[Articles]
    ADD [ImageData] VARBINARY(MAX) NULL;
    
    PRINT 'Колонка ImageData успешно добавлена в таблицу Articles';
END
ELSE
BEGIN
    PRINT 'Колонка ImageData уже существует в таблице Articles';
END
GO
