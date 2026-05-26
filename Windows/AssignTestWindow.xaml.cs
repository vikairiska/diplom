using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VerhozinaIvanovDiplom;

namespace VerhozinaIvanovDiplom.Windows
{
    public partial class AssignTestWindow : Window
    {
        private List<UserPickerItem> _allUsers = new List<UserPickerItem>();

        public AssignTestWindow()
        {
            InitializeComponent();
            InitFilterCombos();
            LoadData();
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
            ApplyUserFilter();
        }

        private void ApplyUserFilter()
        {
            IEnumerable<UserPickerItem> query = _allUsers ?? Enumerable.Empty<UserPickerItem>();

            var spec = FilterSpecialtyCombo.SelectedItem as string;
            if (!string.IsNullOrEmpty(spec) && spec != UserSpecialtyOptions.FilterAllLabel)
            {
                query = query.Where(u =>
                    string.Equals((u.ProfessionalRole ?? string.Empty).Trim(), spec, StringComparison.Ordinal));
            }

            var city = FilterCityCombo.SelectedItem as string;
            if (!string.IsNullOrEmpty(city) && city != UserSpecialtyOptions.FilterAllLabel)
            {
                query = query.Where(u =>
                    string.Equals((u.City ?? string.Empty).Trim(), city, StringComparison.Ordinal));
            }

            var filtered = query.ToList();
            var previousId = UsersComboBox.SelectedValue as int?;

            UsersComboBox.ItemsSource = filtered;

            if (previousId.HasValue && filtered.Any(u => u.Id == previousId.Value))
            {
                UsersComboBox.SelectedValue = previousId.Value;
            }
            else
            {
                UsersComboBox.SelectedIndex = -1;
            }
        }

        private void LoadData()
        {
            using (var context = new DBEntities())
            {
                var userRoleId = context.Roles
                    .Where(r => r.Name == "user")
                    .Select(r => (int?)r.Id)
                    .FirstOrDefault();

                _allUsers = userRoleId.HasValue
                    ? context.Users
                        .Where(u => (u.IsActive ?? true) && u.RoleId == userRoleId.Value)
                        .OrderBy(u => u.FullName)
                        .Select(u => new UserPickerItem
                        {
                            Id = u.Id,
                            DisplayName = (u.FullName ?? u.Login) + " (" + u.Login + ")",
                            ProfessionalRole = u.ProfessionalRole,
                            City = u.City
                        })
                        .ToList()
                    : new List<UserPickerItem>();

                ApplyUserFilter();

                var tests = context.Tests
                    .Where(t =>
                        (t.IsActive ?? true) &&
                        t.ArticleId.HasValue &&
                        context.Articles.Any(a => a.Id == t.ArticleId.Value))
                    .OrderBy(t => t.Title)
                    .Select(t => new PickerItem
                    {
                        Id = t.Id,
                        DisplayName = t.Title
                    })
                    .ToList();

                TestsComboBox.ItemsSource = tests;
            }
        }

        private void AssignButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(UsersComboBox.SelectedValue is int userId) || !(TestsComboBox.SelectedValue is int testId))
            {
                MessageBox.Show("Выберите пользователя и тест.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new DBEntities())
                {
                    bool exists = context.SelectedTests.Any(s => s.UserId == userId && s.TestId == testId);
                    if (exists)
                    {
                        MessageBox.Show("Этот тест уже назначен выбранному пользователю.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    context.SelectedTests.Add(new SelectedTests
                    {
                        UserId = userId,
                        TestId = testId,
                        AssignedDate = DateTime.Now
                    });
                    context.SaveChanges();
                }

                MessageBox.Show("Тест успешно назначен.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка назначения теста: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed class UserPickerItem
        {
            public int Id { get; set; }
            public string DisplayName { get; set; }
            public string ProfessionalRole { get; set; }
            public string City { get; set; }
        }

        private sealed class PickerItem
        {
            public int Id { get; set; }
            public string DisplayName { get; set; }
        }
    }
}
