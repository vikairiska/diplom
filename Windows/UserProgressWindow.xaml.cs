using System;
using System.Linq;
using System.Windows;

namespace VerhozinaIvanovDiplom.Windows
{
    public partial class UserProgressWindow : Window
    {
        private readonly int _userId;

        public UserProgressWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadProgress();
        }

        private void LoadProgress()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    var user = context.Users.FirstOrDefault(u => u.Id == _userId);
                    HeaderTextBlock.Text = user == null
                        ? "Прогресс пользователя"
                        : $"Прогресс: {user.FullName ?? user.Login}";

                    var rawAttempts = (from result in context.TestResults
                                       join test in context.Tests on result.TestId equals test.Id into testJoin
                                       from test in testJoin.DefaultIfEmpty()
                                       where result.UserId == _userId
                                       orderby result.EndDate descending
                                       select new
                                       {
                                           TestTitle = test != null ? test.Title : "Удаленный тест",
                                           result.Score,
                                           result.MaxScore,
                                           result.Passed,
                                           result.EndDate
                                       }).ToList();

                    var attempts = rawAttempts.Select(a => new ProgressRow
                    {
                        TestTitle = a.TestTitle,
                        ScoreText = a.Score + "/" + a.MaxScore,
                        StatusText = a.Passed ? "Пройден" : "Не пройден",
                        EndDateText = a.EndDate.HasValue
                            ? a.EndDate.Value.ToString("dd.MM.yyyy HH:mm")
                            : "-"
                    }).ToList();

                    ProgressDataGrid.ItemsSource = attempts;

                    if (attempts.Count == 0)
                    {
                        SummaryTextBlock.Text = "Попыток тестов пока нет.";
                        return;
                    }

                    int passedCount = attempts.Count(a => a.StatusText == "Пройден");
                    SummaryTextBlock.Text = $"Всего попыток: {attempts.Count}. Успешных: {passedCount}.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки прогресса: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ProgressDataGrid.ItemsSource = null;
                SummaryTextBlock.Text = "Не удалось загрузить прогресс.";
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
