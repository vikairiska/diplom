using System;
using System.Collections.Generic;
using System.IO;
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

namespace VerhozinaIvanovDiplom.Pages
{
    /// <summary>
    /// Конвертер для преобразования байтов изображения в ImageBrush
    /// </summary>
    public class ImageDataToImageBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || !(value is byte[] imageBytes) || imageBytes.Length == 0)
            {
                return new SolidColorBrush(Color.FromRgb(44, 62, 80)); // Цвет по умолчанию #2C3E50
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(imageBytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                var brush = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill
                };
                brush.Freeze();
                return brush;
            }
            catch
            {
                return new SolidColorBrush(Color.FromRgb(44, 62, 80));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Логика взаимодействия для HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadArticles();
        }

        public void RefreshArticles()
        {
            LoadArticles();
        }

        private void LoadArticles()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    var articles = context.Articles
                        .Where(a => a.IsPublished == true)
                        .OrderByDescending(a => a.PublishedDate)
                        .ToList();

                    ArticlesItemsControl.ItemsSource = articles;
                    NoArticlesText.Visibility = articles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ArticleTile_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int articleId)
            {
                // Открытие диалогового окна просмотра статьи
                var viewWindow = new Windows.ArticleViewWindow(articleId);
                viewWindow.Owner = Window.GetWindow(this);
                viewWindow.ShowDialog();
                
                // Обновляем список статей после закрытия окна
                LoadArticles();
            }
        }
    }
}
