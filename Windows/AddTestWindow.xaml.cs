using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VerhozinaIvanovDiplom.Windows
{
    public partial class AddTestWindow : Window
    {
        private readonly int _articleId;
        private readonly int? _testId;
        private readonly List<QuestionBlockState> _questionStates = new List<QuestionBlockState>();
        private int _basicLevelId;

        public AddTestWindow(int articleId, int? testId = null)
        {
            InitializeComponent();
            _articleId = articleId;
            _testId = testId;
            LoadOrInitializeForm();
        }

        private void AddQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            AddQuestionBlock();
        }

        private void LoadOrInitializeForm()
        {
            EnsureLevelsLoaded();

            if (!_testId.HasValue)
            {
                AddQuestionBlock();
                return;
            }

            try
            {
                using (var context = new DBEntities())
                {
                    var test = context.Tests.FirstOrDefault(t => t.Id == _testId.Value && t.ArticleId == _articleId);
                    if (test == null)
                    {
                        AddQuestionBlock();
                        return;
                    }

                    FormTitleTextBlock.Text = "Редактирование теста";
                    Title = "Редактирование теста";
                    TitleTextBox.Text = test.Title;
                    DescriptionTextBox.Text = test.Description;
                    PassingScoreTextBox.Text = test.PassingScore.ToString();
                    TimeLimitTextBox.Text = test.TimeLimitMinutes?.ToString() ?? string.Empty;
                    LevelComboBox.SelectedValue = test.LevelId > 0 ? test.LevelId : _basicLevelId;

                    if (!(LevelComboBox.SelectedValue is int))
                    {
                        LevelComboBox.SelectedValue = _basicLevelId;
                    }

                    var questions = context.Questions
                        .Where(q => q.TestId == test.Id)
                        .OrderBy(q => q.OrderIndex)
                        .ToList();

                    if (questions.Count == 0)
                    {
                        AddQuestionBlock();
                        return;
                    }

                    foreach (var question in questions)
                    {
                        var answers = context.Answers
                            .Where(a => a.QuestionId == question.Id)
                            .OrderBy(a => a.OrderIndex)
                            .Select(a => new AnswerSeed
                            {
                                Text = a.Text,
                                IsCorrect = a.IsCorrect
                            })
                            .ToList();

                        AddQuestionBlock(question.Text, question.QuestionType ?? 1, answers);
                    }
                }
            }
            catch
            {
                AddQuestionBlock();
            }
        }

        private void AddQuestionBlock(string questionText = null, int questionType = 1, List<AnswerSeed> answers = null)
        {
            var state = new QuestionBlockState();
            _questionStates.Add(state);

            var container = new Border
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16)
            };

            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = $"Вопрос {_questionStates.Count}",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Текст вопроса",
                Margin = new Thickness(0, 12, 0, 4)
            });

            state.QuestionTextBox = new TextBox
            {
                Height = 32,
                TextWrapping = TextWrapping.Wrap
            };
            state.QuestionTextBox.Text = questionText ?? string.Empty;
            panel.Children.Add(state.QuestionTextBox);

            var typePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 12, 0, 12)
            };

            typePanel.Children.Add(new TextBlock
            {
                Text = "Тип ответа:",
                VerticalAlignment = VerticalAlignment.Center
            });

            state.TypeComboBox = new ComboBox
            {
                Width = 220,
                Margin = new Thickness(8, 0, 0, 0),
                DisplayMemberPath = "Label",
                SelectedValuePath = "Value",
                Style = (Style)FindResource("StyledComboBoxStyle"),
                ItemContainerStyle = (Style)FindResource("StyledComboBoxItemStyle")
            };
            state.TypeComboBox.Items.Add(new AnswerTypeItem { Label = "Один из списка", Value = 1 });
            state.TypeComboBox.Items.Add(new AnswerTypeItem { Label = "Несколько из списка", Value = 2 });
            state.TypeComboBox.SelectedValue = questionType == 2 ? 2 : 1;
            state.TypeComboBox.SelectionChanged += (sender, args) => UpdateAnswerSelectors(state);

            typePanel.Children.Add(state.TypeComboBox);
            panel.Children.Add(typePanel);

            state.AnswersContainer = new StackPanel();
            panel.Children.Add(state.AnswersContainer);

            var questionActions = new Grid
            {
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            questionActions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            questionActions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var addAnswerButton = new Button
            {
                Content = "Добавить вариант",
                Height = 34,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 8, 0),
                Style = (Style)FindResource("ButtonStyleGreen")
            };
            addAnswerButton.Click += (sender, args) => AddAnswerRow(state);
            Grid.SetColumn(addAnswerButton, 0);
            questionActions.Children.Add(addAnswerButton);

            var deleteQuestionButton = new Button
            {
                Content = "Удалить вопрос",
                Height = 34,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Style = (Style)FindResource("ButtonStyleGreen")
            };
            deleteQuestionButton.Click += (sender, args) => RemoveQuestion(state);
            Grid.SetColumn(deleteQuestionButton, 1);
            questionActions.Children.Add(deleteQuestionButton);

            panel.Children.Add(questionActions);

            container.Child = panel;
            state.Container = container;
            QuestionsContainer.Children.Add(container);

            if (answers != null && answers.Count > 0)
            {
                foreach (var answer in answers)
                {
                    AddAnswerRow(state, answer.Text, answer.IsCorrect);
                }
            }
            else
            {
                AddAnswerRow(state);
                AddAnswerRow(state);
            }

            UpdateQuestionTitles();
        }

        private void RemoveQuestion(QuestionBlockState state)
        {
            _questionStates.Remove(state);
            QuestionsContainer.Children.Remove(state.Container);
            UpdateQuestionTitles();
        }

        private void UpdateQuestionTitles()
        {
            for (int i = 0; i < _questionStates.Count; i++)
            {
                if (_questionStates[i].Container?.Child is StackPanel panel && panel.Children[0] is TextBlock title)
                {
                    title.Text = $"Вопрос {i + 1}";
                }
            }
        }

        private void AddAnswerRow(QuestionBlockState questionState, string answerText = null, bool isCorrect = false)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8),
                VerticalAlignment = VerticalAlignment.Center
            };

            var selector = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var answerTextBox = new TextBox
            {
                Width = 520,
                Height = 30,
                Text = answerText ?? string.Empty
            };

            var deleteButton = new Button
            {
                Content = "X",
                Width = 36,
                Height = 30,
                Margin = new Thickness(8, 0, 0, 0)
            };

            var answerState = new AnswerRowState
            {
                RowContainer = row,
                Selector = selector,
                TextBox = answerTextBox
            };

            deleteButton.Click += (sender, args) =>
            {
                questionState.Answers.Remove(answerState);
                questionState.AnswersContainer.Children.Remove(row);
                EnsureAnswerSelection(questionState);
            };

            if (questionState.Answers.Count == 0 && !isCorrect)
            {
                selector.IsChecked = true;
            }
            else
            {
                selector.IsChecked = isCorrect;
            }

            selector.Checked += (sender, args) =>
            {
                if (GetQuestionTypeValue(questionState) == 1)
                {
                    foreach (var item in questionState.Answers.Where(a => a != answerState))
                    {
                        item.Selector.IsChecked = false;
                    }
                }
            };

            row.Children.Add(selector);
            row.Children.Add(answerTextBox);
            row.Children.Add(deleteButton);

            questionState.Answers.Add(answerState);
            questionState.AnswersContainer.Children.Add(row);
            UpdateAnswerSelectors(questionState);
        }

        private void UpdateAnswerSelectors(QuestionBlockState questionState)
        {
            int type = GetQuestionTypeValue(questionState);
            foreach (var answer in questionState.Answers)
            {
                answer.Selector.Content = type == 1 ? "Правильный" : "Верный";
            }

            if (type == 1)
            {
                EnsureAnswerSelection(questionState);
                var selected = questionState.Answers.FirstOrDefault(a => a.Selector.IsChecked == true);
                if (selected != null)
                {
                    foreach (var answer in questionState.Answers.Where(a => a != selected))
                    {
                        answer.Selector.IsChecked = false;
                    }
                }
            }
        }

        private void EnsureAnswerSelection(QuestionBlockState questionState)
        {
            if (!questionState.Answers.Any(a => a.Selector.IsChecked == true) && questionState.Answers.Count > 0)
            {
                questionState.Answers[0].Selector.IsChecked = true;
            }
        }

        private int GetQuestionTypeValue(QuestionBlockState state)
        {
            if (state.TypeComboBox.SelectedValue is int value)
            {
                return value;
            }

            return 1;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            try
            {
                using (var context = new DBEntities())
                {
                    var levelId = EnsureStandardLevels(context);
                    if (LevelComboBox.SelectedValue is int selectedLevelId)
                    {
                        levelId = selectedLevelId;
                    }

                    Tests test;
                    if (_testId.HasValue)
                    {
                        test = context.Tests.FirstOrDefault(t => t.Id == _testId.Value && t.ArticleId == _articleId);
                        if (test == null)
                        {
                            MessageBox.Show("Тест для редактирования не найден.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    else
                    {
                        test = new Tests
                        {
                            ArticleId = _articleId,
                            IsActive = true
                        };
                        context.Tests.Add(test);
                    }

                    test.LevelId = levelId;
                    test.Title = TitleTextBox.Text.Trim();
                    test.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim();
                    test.PassingScore = ParseIntOrDefault(PassingScoreTextBox.Text, 1);
                    test.TimeLimitMinutes = ParseNullableInt(TimeLimitTextBox.Text);

                    context.SaveChanges();

                    if (_testId.HasValue)
                    {
                        var oldQuestions = context.Questions.Where(q => q.TestId == test.Id).ToList();
                        if (oldQuestions.Count > 0)
                        {
                            var oldQuestionIds = oldQuestions.Select(q => q.Id).ToList();
                            var oldAnswers = context.Answers.Where(a => oldQuestionIds.Contains(a.QuestionId)).ToList();
                            context.Answers.RemoveRange(oldAnswers);
                            context.Questions.RemoveRange(oldQuestions);
                            context.SaveChanges();
                        }
                    }

                    for (int qIndex = 0; qIndex < _questionStates.Count; qIndex++)
                    {
                        var questionState = _questionStates[qIndex];
                        var question = new Questions
                        {
                            TestId = test.Id,
                            Text = questionState.QuestionTextBox.Text.Trim(),
                            QuestionType = (byte)GetQuestionTypeValue(questionState),
                            OrderIndex = qIndex + 1
                        };

                        context.Questions.Add(question);
                        context.SaveChanges();

                        for (int aIndex = 0; aIndex < questionState.Answers.Count; aIndex++)
                        {
                            var answerState = questionState.Answers[aIndex];
                            var answer = new Answers
                            {
                                QuestionId = question.Id,
                                Text = answerState.TextBox.Text.Trim(),
                                IsCorrect = answerState.Selector.IsChecked == true,
                                OrderIndex = aIndex + 1
                            };
                            context.Answers.Add(answer);
                        }
                    }

                    context.SaveChanges();
                }

                MessageBox.Show("Тест успешно сохранен.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении теста: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Введите название теста.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(PassingScoreTextBox.Text, out int passingScore) || passingScore <= 0)
            {
                MessageBox.Show("Проходной балл должен быть целым числом больше 0.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!(LevelComboBox.SelectedValue is int))
            {
                MessageBox.Show("Выберите уровень теста.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TimeLimitTextBox.Text) &&
                (!int.TryParse(TimeLimitTextBox.Text, out int timeLimit) || timeLimit <= 0))
            {
                MessageBox.Show("Лимит времени должен быть пустым или целым числом больше 0.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_questionStates.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один вопрос.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            foreach (var question in _questionStates)
            {
                if (string.IsNullOrWhiteSpace(question.QuestionTextBox.Text))
                {
                    MessageBox.Show("У каждого вопроса должен быть заполнен текст.", "Проверка данных",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (question.Answers.Count < 2)
                {
                    MessageBox.Show("У каждого вопроса должно быть минимум 2 варианта ответа.", "Проверка данных",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (question.Answers.Any(a => string.IsNullOrWhiteSpace(a.TextBox.Text)))
                {
                    MessageBox.Show("Заполните текст всех вариантов ответа.", "Проверка данных",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!question.Answers.Any(a => a.Selector.IsChecked == true))
                {
                    MessageBox.Show("У каждого вопроса должен быть выбран хотя бы один правильный ответ.", "Проверка данных",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private static int ParseIntOrDefault(string value, int defaultValue)
        {
            if (int.TryParse(value, out int parsed))
            {
                return parsed;
            }

            return defaultValue;
        }

        private static int? ParseNullableInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (int.TryParse(value, out int parsed))
            {
                return parsed;
            }

            return null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void EnsureLevelsLoaded()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    _basicLevelId = EnsureStandardLevels(context);
                    var levels = context.TestLevels
                        .OrderBy(t => t.Id)
                        .Select(t => new LevelItem
                        {
                            Id = t.Id,
                            Name = t.Name
                        })
                        .ToList();

                    LevelComboBox.ItemsSource = levels;
                    LevelComboBox.SelectedValue = _basicLevelId;
                }
            }
            catch
            {
                LevelComboBox.ItemsSource = new[]
                {
                    new LevelItem { Id = 1, Name = "Базовый" },
                    new LevelItem { Id = 2, Name = "Продвинутый" },
                    new LevelItem { Id = 3, Name = "Эксперт" }
                };
                LevelComboBox.SelectedIndex = 0;
            }
        }

        private static int EnsureStandardLevels(DBEntities context)
        {
            var basic = context.TestLevels.FirstOrDefault(l => l.Name == "Базовый");
            if (basic == null)
            {
                basic = new TestLevels { Name = "Базовый", MinScoreRequired = 0 };
                context.TestLevels.Add(basic);
                context.SaveChanges();
            }

            var advanced = context.TestLevels.FirstOrDefault(l => l.Name == "Продвинутый");
            if (advanced == null)
            {
                context.TestLevels.Add(new TestLevels { Name = "Продвинутый", MinScoreRequired = 0 });
            }

            var expert = context.TestLevels.FirstOrDefault(l => l.Name == "Эксперт");
            if (expert == null)
            {
                context.TestLevels.Add(new TestLevels { Name = "Эксперт", MinScoreRequired = 0 });
            }

            context.SaveChanges();
            return basic.Id;
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            var previewWindow = new Window
            {
                Title = "Предпросмотр теста",
                Width = 760,
                Height = 620,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(16)
            };

            var root = new StackPanel();
            scroll.Content = root;

            root.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(TitleTextBox.Text) ? "Без названия" : TitleTextBox.Text.Trim(),
                FontSize = 26,
                FontWeight = FontWeights.Bold
            });

            root.Children.Add(new TextBlock
            {
                Text = $"Уровень: {GetSelectedLevelName()}",
                Margin = new Thickness(0, 8, 0, 0),
                FontWeight = FontWeights.SemiBold
            });

            if (!string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                root.Children.Add(new TextBlock
                {
                    Text = DescriptionTextBox.Text.Trim(),
                    Margin = new Thickness(0, 10, 0, 16),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            for (int i = 0; i < _questionStates.Count; i++)
            {
                var question = _questionStates[i];
                var card = new Border
                {
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var panel = new StackPanel();
                card.Child = panel;

                panel.Children.Add(new TextBlock
                {
                    Text = $"Вопрос {i + 1}: {question.QuestionTextBox.Text}",
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                });

                panel.Children.Add(new TextBlock
                {
                    Text = GetQuestionTypeValue(question) == 1 ? "Тип: один из списка" : "Тип: несколько из списка",
                    Margin = new Thickness(0, 6, 0, 8)
                });

                foreach (var answer in question.Answers)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"{(answer.Selector.IsChecked == true ? "[x]" : "[ ]")} {answer.TextBox.Text}",
                        Margin = new Thickness(8, 2, 0, 2),
                        TextWrapping = TextWrapping.Wrap
                    });
                }

                root.Children.Add(card);
            }

            previewWindow.Content = scroll;
            previewWindow.ShowDialog();
        }

        private string GetSelectedLevelName()
        {
            if (LevelComboBox.SelectedItem is LevelItem selected)
            {
                return selected.Name;
            }

            return "Базовый";
        }

        private sealed class QuestionBlockState
        {
            public Border Container { get; set; }
            public TextBox QuestionTextBox { get; set; }
            public ComboBox TypeComboBox { get; set; }
            public StackPanel AnswersContainer { get; set; }
            public List<AnswerRowState> Answers { get; } = new List<AnswerRowState>();
        }

        private sealed class AnswerRowState
        {
            public StackPanel RowContainer { get; set; }
            public CheckBox Selector { get; set; }
            public TextBox TextBox { get; set; }
        }

        private sealed class AnswerTypeItem
        {
            public string Label { get; set; }
            public int Value { get; set; }
        }

        private sealed class AnswerSeed
        {
            public string Text { get; set; }
            public bool IsCorrect { get; set; }
        }

        private sealed class LevelItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
