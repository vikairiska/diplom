using System;
using System.Linq;
using System.Windows;

namespace VerhozinaIvanovDiplom.Windows
{
    public partial class AssignTestWindow : Window
    {
        public AssignTestWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            using (var context = new DBEntities())
            {
                var userRoleId = context.Roles
                    .Where(r => r.Name == "user")
                    .Select(r => (int?)r.Id)
                    .FirstOrDefault();

                var users = userRoleId.HasValue
                    ? context.Users
                        .Where(u => (u.IsActive ?? true) && u.RoleId == userRoleId.Value)
                        .OrderBy(u => u.FullName)
                        .Select(u => new PickerItem
                        {
                            Id = u.Id,
                            DisplayName = (u.FullName ?? u.Login) + " (" + u.Login + ")"
                        })
                        .ToList()
                    : Enumerable.Empty<PickerItem>().ToList();

                var tests = context.Tests
                    .Where(t => t.IsActive ?? true)
                    .OrderBy(t => t.Title)
                    .Select(t => new PickerItem
                    {
                        Id = t.Id,
                        DisplayName = t.Title
                    })
                    .ToList();

                UsersComboBox.ItemsSource = users;
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

        private sealed class PickerItem
        {
            public int Id { get; set; }
            public string DisplayName { get; set; }
        }
    }
}
