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
    internal sealed class CarHotspotZone
    {
        public Rectangle HitSurface { get; }
        public Rect NormalizedOnBitmap { get; }
        public string CarPartName { get; }

        public CarHotspotZone(Rectangle hitSurface, Rect normalizedOnBitmap, string carPartName)
        {
            HitSurface = hitSurface;
            NormalizedOnBitmap = normalizedOnBitmap;
            CarPartName = carPartName;
        }
    }

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
        private List<CarHotspotZone> _carHotspots;
        private string _currentUserCarPartFilter;
        private string _currentTitleFilter;

        public HomePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (SessionContext.IsUser)
            {
                Background = (Brush)FindResource("CarHeroBackgroundBrush");
                ShowUserHeroView();
                UserHeroImage.Source = new BitmapImage(
                    new Uri("pack://application:,,,/Resources/UserHomeHero.png", UriKind.Absolute));
                EnsureCarHotspots();
                LayoutCarHotspots();
            }
            else
            {
                Background = (Brush)FindResource("WindowBackgroundBrush");
                UserBackPanel.Visibility = Visibility.Collapsed;
                UserHeroPanel.Visibility = Visibility.Collapsed;
                UserHeroImage.Source = null;
                ArticlesScrollViewer.Visibility = Visibility.Visible;
                LoadArticles();
            }
        }

        private void UserHeroImage_Loaded(object sender, RoutedEventArgs e)
        {
            if (SessionContext.IsUser)
                LayoutCarHotspots();
        }

        private void UserHeroHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SessionContext.IsUser)
                LayoutCarHotspots();
        }

        /// <summary>
        /// Доли ширины/высоты растрового изображения (0–1). Подгоните под конкретный PNG при необходимости.
        /// </summary>
        private static IEnumerable<(Rect Norm, string Tip, string CarPart, int Z)> GetCarHotspotDefinitions()
        {
            yield return (new Rect(0.06, 0.28, 0.42, 0.34), "Статьи по двигателю", "Двигатель", 20);
            yield return (new Rect(0.02, 0.56, 0.34, 0.40), "Статьи по ходовой части", "Ходовая часть", 10);
            yield return (new Rect(0.64, 0.56, 0.34, 0.40), "Статьи по ходовой части", "Ходовая часть", 10);
            yield return (new Rect(0.28, 0.06, 0.50, 0.26), "Статьи по кузову", "Кузов", 30);
        }

        private void EnsureCarHotspots()
        {
            if (_carHotspots != null)
                return;

            _carHotspots = new List<CarHotspotZone>();
            foreach (var (norm, tip, carPart, z) in GetCarHotspotDefinitions())
            {
                var rect = new Rectangle
                {
                    Fill = Brushes.Transparent,
                    IsHitTestVisible = true,
                    Cursor = Cursors.Help,
                    ToolTip = CreateCarHotspotToolTip(tip)
                };
                rect.MouseLeftButtonUp += CarHotspot_Click;
                Panel.SetZIndex(rect, z);
                HotspotsCanvas.Children.Add(rect);
                _carHotspots.Add(new CarHotspotZone(rect, norm, carPart));
            }
        }

        private ToolTip CreateCarHotspotToolTip(string text)
        {
            var fg = (Brush)FindResource("PrimaryTextBrush");
            var bg = (Brush)FindResource("CardBackgroundBrush");
            var border = (Brush)FindResource("ControlBorderBrush");

            return new ToolTip
            {
                Background = bg,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 8, 12, 8),
                FontSize = 14,
                Content = new TextBlock
                {
                    Text = text,
                    Foreground = fg,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 280
                }
            };
        }

        private void LayoutCarHotspots()
        {
            var bmp = UserHeroImage.Source as BitmapSource;
            if (_carHotspots == null || _carHotspots.Count == 0 || bmp == null)
                return;

            double cw = UserHeroHost.ActualWidth;
            double ch = UserHeroHost.ActualHeight;
            if (cw <= 1 || ch <= 1 || bmp.PixelWidth <= 0 || bmp.PixelHeight <= 0)
                return;

            Rect uniform = ComputeUniformRenderRect(cw, ch, bmp.PixelWidth, bmp.PixelHeight);

            foreach (var zone in _carHotspots)
            {
                Rect n = zone.NormalizedOnBitmap;
                double left = uniform.Left + n.Left * uniform.Width;
                double top = uniform.Top + n.Top * uniform.Height;
                double w = n.Width * uniform.Width;
                double h = n.Height * uniform.Height;

                Canvas.SetLeft(zone.HitSurface, left);
                Canvas.SetTop(zone.HitSurface, top);
                zone.HitSurface.Width = Math.Max(1, w);
                zone.HitSurface.Height = Math.Max(1, h);
            }
        }

        private static Rect ComputeUniformRenderRect(double containerW, double containerH, double imgW, double imgH)
        {
            if (containerW <= 0 || containerH <= 0 || imgW <= 0 || imgH <= 0)
                return new Rect(0, 0, 0, 0);

            double scale = Math.Min(containerW / imgW, containerH / imgH);
            double rw = imgW * scale;
            double rh = imgH * scale;
            double ox = (containerW - rw) / 2;
            double oy = (containerH - rh) / 2;
            return new Rect(ox, oy, rw, rh);
        }

        private void ShowUserHeroView()
        {
            _currentUserCarPartFilter = null;
            _currentTitleFilter = null;
            UserBackPanel.Visibility = Visibility.Collapsed;
            ArticlesScrollViewer.Visibility = Visibility.Collapsed;
            NoArticlesText.Visibility = Visibility.Collapsed;
            UserHeroPanel.Visibility = Visibility.Visible;
        }

        private void ShowUserFilteredArticlesView(string carPartName, string titleFilter = null)
        {
            _currentUserCarPartFilter = carPartName;
            _currentTitleFilter = titleFilter;
            UserBackPanel.Visibility = Visibility.Visible;
            UserHeroPanel.Visibility = Visibility.Collapsed;
            ArticlesScrollViewer.Visibility = Visibility.Visible;
            Background = (Brush)FindResource("WindowBackgroundBrush");
            LoadArticles(carPartName, titleFilter);
        }

        private void CarHotspot_Click(object sender, MouseButtonEventArgs e)
        {
            if (!SessionContext.IsUser)
                return;

            var rect = sender as Rectangle;
            var zone = _carHotspots?.FirstOrDefault(z => z.HitSurface == rect);
            if (zone == null || string.IsNullOrWhiteSpace(zone.CarPartName))
                return;

            ShowUserFilteredArticlesView(zone.CarPartName);
            e.Handled = true;
        }

        private void BackToCarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionContext.IsUser)
                return;

            Background = (Brush)FindResource("CarHeroBackgroundBrush");
            ShowUserHeroView();
            LayoutCarHotspots();
        }

        public void RefreshArticles()
        {
            if (SessionContext.IsUser)
            {
                if (!string.IsNullOrWhiteSpace(_currentUserCarPartFilter) || !string.IsNullOrWhiteSpace(_currentTitleFilter))
                    LoadArticles(_currentUserCarPartFilter, _currentTitleFilter);
            }
            else
            {
                LoadArticles(titleFilter: _currentTitleFilter);
            }
        }

        public void ApplyTitleFilter(string titleFilter)
        {
            string normalizedFilter = string.IsNullOrWhiteSpace(titleFilter) ? null : titleFilter.Trim();

            if (SessionContext.IsUser)
            {
                ShowUserFilteredArticlesView(null, normalizedFilter);
            }
            else
            {
                _currentTitleFilter = normalizedFilter;
                LoadArticles(titleFilter: normalizedFilter);
            }
        }

        private void LoadArticles(string carPartName = null, string titleFilter = null)
        {
            try
            {
                using (var context = new DBEntities())
                {
                    var query = context.Articles.Where(a => a.IsPublished == true);

                    if (!string.IsNullOrWhiteSpace(carPartName))
                        query = query.Where(a => a.CarParts != null && a.CarParts.Name == carPartName);

                    if (!string.IsNullOrWhiteSpace(titleFilter))
                        query = query.Where(a => a.Title.Contains(titleFilter));

                    var articles = query
                        .OrderByDescending(a => a.PublishedDate)
                        .ToList();

                    ArticlesItemsControl.ItemsSource = articles;
                    NoArticlesText.Visibility = articles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    if (!string.IsNullOrWhiteSpace(carPartName))
                        NoArticlesText.Text = $"По части \"{carPartName}\" статей пока нет";
                    else if (!string.IsNullOrWhiteSpace(titleFilter))
                        NoArticlesText.Text = $"По запросу \"{titleFilter}\" статьи не найдены";
                    else
                        NoArticlesText.Text = "Статьи пока не добавлены";
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
                RefreshArticles();
            }
        }
    }
}
