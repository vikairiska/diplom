using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VerhozinaIvanovDiplom.Windows;

namespace VerhozinaIvanovDiplom
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ApplyRolePermissions();
            // Добавление обработчиков для кнопок меню
            AddHandler(Button.ClickEvent, new RoutedEventHandler(OnMenuButtonClick));
        }

        private void ApplyRolePermissions()
        {
            if (SessionContext.IsAdmin)
            {
                MyProfileMenuButton.Visibility = Visibility.Collapsed;
                AssignedTestsMenuButton.Visibility = Visibility.Collapsed;
                return;
            }

            AddMenuButton.Visibility = Visibility.Collapsed;
            AssignMenuButton.Visibility = Visibility.Collapsed;
            UsersMenuButton.Visibility = Visibility.Collapsed;
            SearchMenuButton.Visibility = Visibility.Collapsed;
            UsersProgressMenuButton.Visibility = Visibility.Collapsed;
        }

        private void OnMenuButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button button)
            {
                switch (button.Content)
                {
                    case "Добавить":
                        var addWindow = new Windows.AddArticleWindow();
                        addWindow.Owner = this;
                        addWindow.ShowDialog();
                        
                        // Обновляем список статей после добавления
                        if (this.Content is Grid grid)
                        {
                            var frame = grid.Children.OfType<System.Windows.Controls.Frame>().FirstOrDefault();
                            if (frame?.Content is Pages.HomePage homePage)
                            {
                                homePage.RefreshArticles();
                            }
                        }
                        break;
                    case "Назначить":
                        OpenAssignTests();
                        break;
                    case "Пользователи":
                        // TODO: Открытие окна пользователей
                        break;
                    case "Найти":
                        // TODO: Логика поиска
                        break;
                    case "Мой профиль":
                        OpenMyProfile();
                        break;
                    case "Назначенные тесты":
                        OpenAssignedTests();
                        break;
                    case "Прогресс пользователей":
                        OpenUsersProgress();
                        break;
                    case "Выйти":
                        Logout();
                        break;
                }
            }
        }

        private void OpenMyProfile()
        {
            if (!SessionContext.CurrentUserId.HasValue)
            {
                MessageBox.Show("Пользователь не определен.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var profileWindow = new UserProfileWindow(SessionContext.CurrentUserId.Value);
            profileWindow.Owner = this;
            profileWindow.ShowDialog();
        }

        private void OpenUsersProgress()
        {
            var usersProgressWindow = new UsersProgressWindow();
            usersProgressWindow.Owner = this;
            usersProgressWindow.ShowDialog();
        }

        private void OpenAssignTests()
        {
            var assignWindow = new AssignTestWindow();
            assignWindow.Owner = this;
            assignWindow.ShowDialog();
        }

        private void OpenAssignedTests()
        {
            if (!SessionContext.CurrentUserId.HasValue)
            {
                MessageBox.Show("Пользователь не определен.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var assignedTestsWindow = new AssignedTestsWindow(SessionContext.CurrentUserId.Value);
            assignedTestsWindow.Owner = this;
            assignedTestsWindow.ShowDialog();
        }

        private void Logout()
        {
            SessionContext.Clear();

            var loginWindow = new LoginWindow();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
            Close();
        }
    }
}
