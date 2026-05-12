using System;
using System.Windows;
using System.Windows.Media.Imaging;
using VerhozinaIvanovDiplom;

namespace VerhozinaIvanovDiplom.Windows
{
    public partial class ArticlePreviewWindow : Window
    {
        public ArticlePreviewWindow(string title, string content, BitmapImage coverImage)
        {
            InitializeComponent();

            TitleTextBlock.Text = string.IsNullOrWhiteSpace(title) ? "Без названия" : title;
            ArticleRichContentRenderer.Render(ContentPanel, content ?? string.Empty);

            if (coverImage != null)
            {
                ArticleImage.Source = coverImage;
                CoverBorder.Visibility = Visibility.Visible;
            }
            else
            {
                CoverBorder.Visibility = Visibility.Collapsed;
                ArticleBodyGrid.RowDefinitions[0].Height = new GridLength(0);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
