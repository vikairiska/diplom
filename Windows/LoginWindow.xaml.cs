using System;
using System.Windows;
using System.Windows.Controls;

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
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Заглушка - переход в основное окно
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Завершение работы приложения
            Application.Current.Shutdown();
        }

        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            // Заглушка - окно регистрации
            MessageBox.Show("Окно регистрации будет реализовано позже", "Регистрация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
