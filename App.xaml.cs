using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VerhozinaIvanovDiplom.Windows;

namespace VerhozinaIvanovDiplom
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            EnsureSelectedTestsTable();
            EnsureUsersProfessionalColumns();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }

        private static void EnsureSelectedTestsTable()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    context.Database.ExecuteSqlCommand(@"
IF OBJECT_ID(N'[dbo].[SelectedTests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SelectedTests](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [TestId] INT NOT NULL,
        [AssignedDate] DATETIME NOT NULL CONSTRAINT [DF_SelectedTests_AssignedDate] DEFAULT (GETDATE())
    );
END
");
                }
            }
            catch
            {
                // Ошибка будет видна при попытке назначить тесты.
            }
        }

        /// <summary>
        /// Синхронизация схемы с моделью EF: поля специализации и города (см. AddUserProfessionalRoleAndCity.sql).
        /// </summary>
        private static void EnsureUsersProfessionalColumns()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    context.Database.ExecuteSqlCommand(@"
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH('dbo.Users', N'ProfessionalRole') IS NULL
    ALTER TABLE dbo.Users ADD ProfessionalRole NVARCHAR(100) NULL;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH('dbo.Users', N'City') IS NULL
    ALTER TABLE dbo.Users ADD City NVARCHAR(100) NULL;
");
                }
            }
            catch
            {
                // Ошибка проявится при сохранении пользователя; при необходимости выполните SQL вручную.
            }
        }
    }
}
