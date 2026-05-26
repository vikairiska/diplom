using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VerhozinaIvanovDiplom;

namespace VerhozinaIvanovDiplom.Windows
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            EnsureDefaultUsers();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var login = LoginTextBox.Text?.Trim();
            var password = (PasswordBox.Password ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Авторизация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new DBEntities())
                {
                    var userCandidates = context.Users
                        .Include(u => u.Roles)
                        .Where(u => u.Login == login && (u.IsActive ?? true))
                        .OrderByDescending(u => u.Id)
                        .ToList();

                    var user = userCandidates.FirstOrDefault(u =>
                        string.Equals((u.PasswordHash ?? string.Empty).Trim(), password, StringComparison.Ordinal));

                    if (user == null)
                    {
                        MessageBox.Show("Неверный логин или пароль.", "Авторизация",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    SessionContext.CurrentUserId = user.Id;
                    SessionContext.CurrentUserLogin = user.Login;
                    SessionContext.CurrentRole = user.Roles?.Name;
                }

                var mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Завершение работы приложения
            Application.Current.Shutdown();
        }

        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegistrationWindow
            {
                Owner = this
            };

            var isRegistered = registerWindow.ShowDialog();
            if (isRegistered == true)
            {
                LoginTextBox.Focus();
                LoginTextBox.SelectAll();
                PasswordBox.Clear();
            }
        }

        private static void EnsureDefaultUsers()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    var adminRole = context.Roles.FirstOrDefault(r => r.Name == "admin");
                    if (adminRole == null)
                    {
                        adminRole = new Roles
                        {
                            Name = "admin",
                            Description = "Администратор"
                        };
                        context.Roles.Add(adminRole);
                        context.SaveChanges();
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

                    var adminUser = context.Users.FirstOrDefault(u => u.Login == "admin");
                    if (adminUser == null)
                    {
                        context.Users.Add(new Users
                        {
                            Login = "admin",
                            PasswordHash = "admin",
                            FullName = "Administrator",
                            RoleId = adminRole.Id,
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        });
                    }

                    var regularUser = context.Users.FirstOrDefault(u => u.Login == "user");
                    if (regularUser == null)
                    {
                        context.Users.Add(new Users
                        {
                            Login = "user",
                            PasswordHash = "user",
                            FullName = "User",
                            RoleId = userRole.Id,
                            IsActive = true,
                            CreatedDate = DateTime.Now,
                            ProfessionalRole = UserSpecialtyOptions.Specialties[2],
                            City = UserSpecialtyOptions.Cities[0]
                        });
                    }

                    context.SaveChanges();
                }
            }
            catch
            {
                // В случае проблем с БД окно авторизации все равно откроется,
                // а ошибка будет показана при попытке входа.
            }
        }
    }
}
