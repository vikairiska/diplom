using System;
using System.Linq;
using System.Windows;

namespace VerhozinaIvanovDiplom.Windows
{
    /// <summary>
    /// Логика взаимодействия для RegistrationWindow.xaml
    /// </summary>
    public partial class RegistrationWindow : Window
    {
        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var login = LoginTextBox.Text?.Trim();
            var fullName = FullNameTextBox.Text?.Trim();
            var password = PasswordBox.Password?.Trim() ?? string.Empty;
            var confirmPassword = ConfirmPasswordBox.Password?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Введите логин.", "Регистрация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LoginTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите пароль.", "Регистрация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            if (password.Length < 4)
            {
                MessageBox.Show("Пароль должен быть не короче 4 символов.", "Регистрация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                MessageBox.Show("Пароли не совпадают.", "Регистрация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfirmPasswordBox.Focus();
                return;
            }

            try
            {
                using (var context = new DBEntities())
                {
                    var loginExists = context.Users.Any(u => u.Login == login);
                    if (loginExists)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует.", "Регистрация",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        LoginTextBox.Focus();
                        return;
                    }

                    var userRole = context.Roles.FirstOrDefault(r => r.Name == "user");
                    if (userRole == null)
                    {
                        userRole = new Roles
                        {
                            Name = "user",
                            Description = "Пользователь"
                        };
                        context.Roles.Add(userRole);
                        context.SaveChanges();
                    }

                    context.Users.Add(new Users
                    {
                        Login = login,
                        PasswordHash = password,
                        FullName = string.IsNullOrWhiteSpace(fullName) ? login : fullName,
                        RoleId = userRole.Id,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    });

                    context.SaveChanges();
                }

                MessageBox.Show("Регистрация прошла успешно. Теперь войдите в систему.", "Регистрация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
