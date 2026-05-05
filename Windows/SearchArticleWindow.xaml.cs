using System.Windows;

namespace VerhozinaIvanovDiplom.Windows
{
    /// <summary>
    /// Логика взаимодействия для SearchArticleWindow.xaml
    /// </summary>
    public partial class SearchArticleWindow : Window
    {
        public string SearchText { get; private set; }

        public SearchArticleWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchText = SearchTextBox.Text?.Trim();
            DialogResult = true;
            Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SearchText = null;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
