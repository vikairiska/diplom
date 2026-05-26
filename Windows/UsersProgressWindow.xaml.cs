using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using VerhozinaIvanovDiplom;

namespace VerhozinaIvanovDiplom.Windows
{
    public partial class UsersProgressWindow : Window
    {
        private List<UserRow> _allRows = new List<UserRow>();

        public UsersProgressWindow()
        {
            InitializeComponent();
            InitFilterCombos();
            LoadUsers();
        }

        private void InitFilterCombos()
        {
            var spec = new List<string> { UserSpecialtyOptions.FilterAllLabel };
            spec.AddRange(UserSpecialtyOptions.Specialties);
            FilterSpecialtyCombo.ItemsSource = spec;
            FilterSpecialtyCombo.SelectedIndex = 0;

            var cities = new List<string> { UserSpecialtyOptions.FilterAllLabel };
            cities.AddRange(UserSpecialtyOptions.Cities);
            FilterCityCombo.ItemsSource = cities;
            FilterCityCombo.SelectedIndex = 0;
        }

        private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allRows == null)
            {
                return;
            }

            IEnumerable<UserRow> query = _allRows;

            var spec = FilterSpecialtyCombo.SelectedItem as string;
            if (!string.IsNullOrEmpty(spec) && spec != UserSpecialtyOptions.FilterAllLabel)
            {
                query = query.Where(r =>
                    string.Equals((r.ProfessionalRoleRaw ?? string.Empty).Trim(), spec, StringComparison.Ordinal));
            }

            var city = FilterCityCombo.SelectedItem as string;
            if (!string.IsNullOrEmpty(city) && city != UserSpecialtyOptions.FilterAllLabel)
            {
                query = query.Where(r =>
                    string.Equals((r.CityRaw ?? string.Empty).Trim(), city, StringComparison.Ordinal));
            }

            UsersListView.ItemsSource = query.ToList();
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
                        _allRows = new List<UserRow>();
                        ApplyFilters();
                        return;
                    }

                    var users = context.Users
                        .Where(u => (u.IsActive ?? true) && u.RoleId == userRoleId.Value)
                        .OrderBy(u => u.FullName)
                        .Select(u => new { u.Id, u.Login, u.FullName, u.ProfessionalRole, u.City })
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
                            Specialty = string.IsNullOrWhiteSpace(user.ProfessionalRole) ? "—" : user.ProfessionalRole,
                            City = string.IsNullOrWhiteSpace(user.City) ? "—" : user.City,
                            ProfessionalRoleRaw = user.ProfessionalRole,
                            CityRaw = user.City,
                            Attempts = attempts,
                            Passed = passed,
                            Photo = ToImage(profile.PhotoBase64)
                        });
                    }

                    _allRows = rows;
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _allRows = new List<UserRow>();
                ApplyFilters();
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
            public string Specialty { get; set; }
            public string City { get; set; }
            public string ProfessionalRoleRaw { get; set; }
            public string CityRaw { get; set; }
            public int Attempts { get; set; }
            public int Passed { get; set; }
            public BitmapImage Photo { get; set; }
        }
    }
}
