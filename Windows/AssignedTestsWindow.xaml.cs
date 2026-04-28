using System.Linq;
using System.Windows;

namespace VerhozinaIvanovDiplom.Windows
{
    public partial class AssignedTestsWindow : Window
    {
        private readonly int _userId;

        public AssignedTestsWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadAssignedTests();
        }

        private void LoadAssignedTests()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    var rows = (from selected in context.SelectedTests
                                join test in context.Tests on selected.TestId equals test.Id
                                where selected.UserId == _userId
                                orderby selected.AssignedDate descending
                                select new AssignedTestRow
                                {
                                    SelectedTestId = selected.Id,
                                    TestId = test.Id,
                                    TestTitle = test.Title,
                                    Description = test.Description,
                                    AssignedDate = selected.AssignedDate
                                }).ToList()
                        .Select(r =>
                        {
                            r.AssignedDateText = r.AssignedDate.ToString("dd.MM.yyyy HH:mm");
                            return r;
                        })
                        .ToList();

                    AssignedTestsGrid.ItemsSource = rows;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки назначенных тестов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                AssignedTestsGrid.ItemsSource = null;
            }
        }

        private void TakeSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(AssignedTestsGrid.SelectedItem is AssignedTestRow row))
            {
                MessageBox.Show("Выберите тест из списка.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var takeWindow = new TakeTestWindow(row.TestId, _userId, row.SelectedTestId);
            takeWindow.Owner = this;
            var result = takeWindow.ShowDialog();

            if (result == true)
            {
                LoadAssignedTests();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAssignedTests();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed class AssignedTestRow
        {
            public int SelectedTestId { get; set; }
            public int TestId { get; set; }
            public string TestTitle { get; set; }
            public string Description { get; set; }
            public System.DateTime AssignedDate { get; set; }
            public string AssignedDateText { get; set; }
        }
    }
}
