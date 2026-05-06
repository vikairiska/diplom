using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VerhozinaIvanovDiplom.Windows
{
    public partial class TakeTestWindow : Window
    {
        private readonly int _testId;
        private readonly int _userId;
        private readonly int? _selectedTestId;
        private readonly DateTime _startDate;
        private readonly List<QuestionUiState> _questionStates = new List<QuestionUiState>();
        private int _passingScore;

        public TakeTestWindow(int testId, int userId, int? selectedTestId = null)
        {
            InitializeComponent();
            _testId = testId;
            _userId = userId;
            _selectedTestId = selectedTestId;
            _startDate = DateTime.Now;
            LoadTest();
        }

        private void LoadTest()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    var test = context.Tests.FirstOrDefault(t => t.Id == _testId);
                    if (test == null)
                    {
                        MessageBox.Show("Тест не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        Close();
                        return;
                    }

                    var articleId = test.ArticleId ?? 0;
                    if (SessionContext.IsUser && articleId > 0 && !SessionContext.IsArticleRead(articleId))
                    {
                        MessageBox.Show("Перед прохождением теста откройте и прочитайте соответствующую статью.",
                            "Доступ к тесту ограничен",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        Close();
                        return;
                    }

                    _passingScore = test.PassingScore;
                    TestTitleTextBlock.Text = test.Title;
                    var levelName = context.TestLevels
                        .Where(l => l.Id == test.LevelId)
                        .Select(l => l.Name)
                        .FirstOrDefault();
                    TestLevelTextBlock.Text = $"Уровень: {(string.IsNullOrWhiteSpace(levelName) ? "Базовый" : levelName)}";
                    TestDescriptionTextBlock.Text = test.Description;

                    var questions = context.Questions
                        .Where(q => q.TestId == _testId)
                        .OrderBy(q => q.OrderIndex)
                        .ToList();

                    if (questions.Count == 0)
                    {
                        MessageBox.Show("В тесте нет вопросов.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        Close();
                        return;
                    }

                    foreach (var question in questions)
                    {
                        var answers = context.Answers
                            .Where(a => a.QuestionId == question.Id)
                            .OrderBy(a => a.OrderIndex)
                            .ToList();

                        BuildQuestionUi(question, answers);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки теста: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void BuildQuestionUi(Questions question, List<Answers> answers)
        {
            var card = new Border
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var panel = new StackPanel();
            card.Child = panel;

            panel.Children.Add(new TextBlock
            {
                Text = question.Text,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            bool isSingleChoice = (question.QuestionType ?? 1) == 1;
            if (isSingleChoice)
            {
                var groupName = "Question" + question.Id;
                foreach (var answer in answers)
                {
                    var radio = new RadioButton
                    {
                        Content = answer.Text,
                        Tag = answer,
                        GroupName = groupName,
                        Margin = new Thickness(0, 8, 0, 0)
                    };
                    panel.Children.Add(radio);
                }
            }
            else
            {
                foreach (var answer in answers)
                {
                    var check = new CheckBox
                    {
                        Content = answer.Text,
                        Tag = answer,
                        Margin = new Thickness(0, 8, 0, 0)
                    };
                    panel.Children.Add(check);
                }
            }

            QuestionsContainer.Children.Add(card);
            _questionStates.Add(new QuestionUiState
            {
                Question = question,
                Container = panel
            });
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAnswers())
            {
                return;
            }

            int score = CalculateScore();
            int maxScore = _questionStates.Count;
            bool passed = score >= _passingScore;

            try
            {
                using (var context = new DBEntities())
                {
                    context.TestResults.Add(new TestResults
                    {
                        UserId = _userId,
                        TestId = _testId,
                        Score = score,
                        MaxScore = maxScore,
                        Passed = passed,
                        StartDate = _startDate,
                        EndDate = DateTime.Now
                    });

                    if (_selectedTestId.HasValue)
                    {
                        var selected = context.SelectedTests.FirstOrDefault(s => s.Id == _selectedTestId.Value && s.UserId == _userId);
                        if (selected != null)
                        {
                            context.SelectedTests.Remove(selected);
                        }
                    }
                    context.SaveChanges();
                }

                MessageBox.Show(
                    $"Результат: {score} из {maxScore}\nСтатус: {(passed ? "Тест пройден" : "Тест не пройден")}",
                    "Результат теста",
                    MessageBoxButton.OK,
                    passed ? MessageBoxImage.Information : MessageBoxImage.Warning);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения результата: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateAnswers()
        {
            foreach (var state in _questionStates)
            {
                bool isSingleChoice = (state.Question.QuestionType ?? 1) == 1;
                bool hasAnySelection = isSingleChoice
                    ? state.Container.Children.OfType<RadioButton>().Any(r => r.IsChecked == true)
                    : state.Container.Children.OfType<CheckBox>().Any(c => c.IsChecked == true);

                if (!hasAnySelection)
                {
                    MessageBox.Show("Ответьте на все вопросы перед завершением теста.", "Проверка данных",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private int CalculateScore()
        {
            int score = 0;

            foreach (var state in _questionStates)
            {
                var controls = state.Container.Children.Cast<object>().Skip(1).ToList();
                bool isSingleChoice = (state.Question.QuestionType ?? 1) == 1;

                if (isSingleChoice)
                {
                    var selected = controls.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true)?.Tag as Answers;
                    if (selected != null && selected.IsCorrect)
                    {
                        score++;
                    }
                }
                else
                {
                    var selectedIds = controls
                        .OfType<CheckBox>()
                        .Where(c => c.IsChecked == true && c.Tag is Answers)
                        .Select(c => ((Answers)c.Tag).Id)
                        .OrderBy(id => id)
                        .ToList();

                    var correctIds = controls
                        .OfType<CheckBox>()
                        .Where(c => c.Tag is Answers && ((Answers)c.Tag).IsCorrect)
                        .Select(c => ((Answers)c.Tag).Id)
                        .OrderBy(id => id)
                        .ToList();

                    if (selectedIds.SequenceEqual(correctIds))
                    {
                        score++;
                    }
                }
            }

            return score;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed class QuestionUiState
        {
            public Questions Question { get; set; }
            public StackPanel Container { get; set; }
        }
    }
}
