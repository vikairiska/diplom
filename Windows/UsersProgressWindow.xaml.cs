using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace VerhozinaIvanovDiplom.Windows
{
    public partial class UsersProgressWindow : Window
    {
        public UsersProgressWindow()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    var userRoleId = context.Roles
                        .Where(r => r.Name == "user")
                        .Select(r => (int?)r.Id)
                        .FirstOrDefault();

                    if (!userRoleId.HasValue)
                    {
                        UsersListView.ItemsSource = new List<UserRow>();
                        return;
                    }

                    var users = context.Users
                        .Where(u => (u.IsActive ?? true) && u.RoleId == userRoleId.Value)
                        .OrderBy(u => u.FullName)
                        .Select(u => new { u.Id, u.Login, u.FullName })
                        .ToList();

                    var rows = new List<UserRow>();

                    foreach (var user in users)
                    {
                        var profile = UserProfileStore.Get(user.Id);
                        var attempts = context.TestResults.Count(r => r.UserId == user.Id);
                        var passed = context.TestResults.Count(r => r.UserId == user.Id && r.Passed);

                        rows.Add(new UserRow
                        {
                            UserId = user.Id,
                            Name = string.IsNullOrWhiteSpace(user.FullName) ? user.Login : user.FullName,
                            Login = user.Login,
                            Attempts = attempts,
                            Passed = passed,
                            Photo = ToImage(profile.PhotoBase64)
                        });
                    }

                    UsersListView.ItemsSource = rows;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UsersListView.ItemsSource = new List<UserRow>();
            }
        }

        private void OpenProgressButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(UsersListView.SelectedItem is UserRow row))
            {
                MessageBox.Show("Выберите пользователя из списка.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var progressWindow = new UserProgressWindow(row.UserId);
            progressWindow.Owner = this;
            progressWindow.ShowDialog();
        }

        private static BitmapImage ToImage(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
            {
                return null;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed class UserRow
        {
            public int UserId { get; set; }
            public string Name { get; set; }
            public string Login { get; set; }
            public int Attempts { get; set; }
            public int Passed { get; set; }
            public BitmapImage Photo { get; set; }
        }
    }
}
