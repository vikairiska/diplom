using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace VerhozinaIvanovDiplom.Windows
{
    public partial class UserProfileWindow : Window
    {
        private readonly int _userId;
        private string _photoBase64;

        public UserProfileWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadProfile();
            LoadProgress();
        }

        private void LoadProfile()
        {
            using (var context = new DBEntities())
            {
                var user = context.Users.FirstOrDefault(u => u.Id == _userId);
                if (user == null)
                {
                    MessageBox.Show("Пользователь не найден.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                FullNameTextBox.Text = user.FullName;
                LoginTextBox.Text = user.Login;
            }

            var profile = UserProfileStore.Get(_userId);
            AgeTextBox.Text = profile.Age?.ToString() ?? string.Empty;
            PositionTextBox.Text = profile.Position ?? string.Empty;
            _photoBase64 = profile.PhotoBase64;
            SetPhotoFromBase64(_photoBase64);
        }

        private void LoadProgress()
        {
            using (var context = new DBEntities())
            {
                var rawAttempts = (from result in context.TestResults
                                   join test in context.Tests on result.TestId equals test.Id
                                   where result.UserId == _userId
                                   orderby result.EndDate descending
                                   select new
                                   {
                                       test.Title,
                                       result.Score,
                                       result.MaxScore,
                                       result.Passed,
                                       result.EndDate
                                   }).ToList();

                var attempts = rawAttempts.Select(a => new ProgressRow
                {
                    TestTitle = a.Title,
                    ScoreText = a.Score + "/" + a.MaxScore,
                    StatusText = a.Passed ? "Пройден" : "Не пройден",
                    EndDateText = a.EndDate.HasValue
                        ? a.EndDate.Value.ToString("dd.MM.yyyy HH:mm")
                        : "-"
                }).ToList();

                ProgressDataGrid.ItemsSource = attempts;
            }
        }

        private void ChoosePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                var bytes = File.ReadAllBytes(dialog.FileName);
                _photoBase64 = Convert.ToBase64String(bytes);
                SetPhotoFromBase64(_photoBase64);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(AgeTextBox.Text) &&
                (!int.TryParse(AgeTextBox.Text, out int age) || age <= 0 || age > 120))
            {
                MessageBox.Show("Возраст должен быть пустым или числом от 1 до 120.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = new DBEntities())
            {
                var user = context.Users.FirstOrDefault(u => u.Id == _userId);
                if (user == null)
                {
                    MessageBox.Show("Пользователь не найден.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                user.FullName = string.IsNullOrWhiteSpace(FullNameTextBox.Text)
                    ? user.Login
                    : FullNameTextBox.Text.Trim();
                context.SaveChanges();
            }

            UserProfileStore.Save(_userId, new UserProfileData
            {
                Age = string.IsNullOrWhiteSpace(AgeTextBox.Text) ? (int?)null : int.Parse(AgeTextBox.Text),
                Position = string.IsNullOrWhiteSpace(PositionTextBox.Text) ? null : PositionTextBox.Text.Trim(),
                PhotoBase64 = _photoBase64
            });

            MessageBox.Show("Профиль сохранен.", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenDetailedProgressButton_Click(object sender, RoutedEventArgs e)
        {
            var progressWindow = new UserProgressWindow(_userId);
            progressWindow.Owner = this;
            progressWindow.ShowDialog();
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var currentPassword = (CurrentPasswordBox.Password ?? string.Empty).Trim();
            var newPassword = (NewPasswordBox.Password ?? string.Empty).Trim();
            var confirmPassword = (ConfirmPasswordBox.Password ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Заполните все поля для смены пароля.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword.Length < 4)
            {
                MessageBox.Show("Новый пароль должен содержать минимум 4 символа.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Подтверждение пароля не совпадает.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = new DBEntities())
            {
                var user = context.Users.FirstOrDefault(u => u.Id == _userId);
                if (user == null)
                {
                    MessageBox.Show("Пользователь не найден.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var storedPassword = (user.PasswordHash ?? string.Empty).Trim();
                if (!string.Equals(storedPassword, currentPassword, StringComparison.Ordinal))
                {
                    MessageBox.Show("Текущий пароль введен неверно.", "Проверка данных",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Если в БД есть дубли логина, меняем пароль у всех,
                // чтобы вход был консистентным для одного логина.
                var usersWithSameLogin = context.Users.Where(u => u.Login == user.Login).ToList();
                foreach (var sameLoginUser in usersWithSameLogin)
                {
                    sameLoginUser.PasswordHash = newPassword;
                }
                context.SaveChanges();
            }

            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();

            MessageBox.Show("Пароль успешно изменен.", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetPhotoFromBase64(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
            {
                PhotoImage.Source = null;
                return;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                PhotoImage.Source = bitmap;
            }
            catch
            {
                PhotoImage.Source = null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed class ProgressRow
        {
            public string TestTitle { get; set; }
            public string ScoreText { get; set; }
            public string StatusText { get; set; }
            public string EndDateText { get; set; }
        }
    }
}
