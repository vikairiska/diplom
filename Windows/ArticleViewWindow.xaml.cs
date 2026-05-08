using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VerhozinaIvanovDiplom.Windows
{
    /// <summary>
    /// Логика взаимодействия для ArticleViewWindow.xaml
    /// </summary>
    public partial class ArticleViewWindow : Window
    {
        private readonly int _articleId;
        private Articles _article;

        public ArticleViewWindow(int articleId)
        {
            InitializeComponent();
            _articleId = articleId;
            ApplyRolePermissions();
            LoadArticle();
            LoadComments();
        }

        private void ApplyRolePermissions()
        {
            if (SessionContext.IsAdmin)
            {
                PassTestButton.Visibility = Visibility.Collapsed;
                return;
            }

            AddTestButton.Visibility = Visibility.Collapsed;
            EditButton.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Collapsed;
            PassTestButton.Visibility = Visibility.Visible;
        }

        private void LoadArticle()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    var article = context.Articles.FirstOrDefault(a => a.Id == _articleId);
                    
                    if (article != null)
                    {
                        _article = article;
                        if (SessionContext.IsUser)
                            SessionContext.MarkArticleAsRead(_articleId);

                        TitleTextBlock.Text = article.Title;
                        RenderArticleContent(article.Content);

                        // Загрузка изображения
                        if (article.ImageData != null && article.ImageData.Length > 0)
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = new MemoryStream(article.ImageData);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            ArticleImage.Source = bitmap;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Статья не найдена.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статьи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadComments()
        {
            try
            {
                using (var context = new DBEntities())
                {
                    var rawComments = (from comment in context.ArticleComments
                                       join user in context.Users on comment.UserId equals user.Id
                                       where comment.ArticleId == _articleId
                                       orderby comment.CreatedDate descending
                                       select new
                                       {
                                           comment.Id,
                                           comment.UserId,
                                           UserName = user.FullName,
                                           UserLogin = user.Login,
                                           comment.Text,
                                           comment.CreatedDate
                                       }).ToList();

                    var items = rawComments.Select(c => new CommentItem
                    {
                        CommentId = c.Id,
                        Header = $"{(string.IsNullOrWhiteSpace(c.UserName) ? c.UserLogin : c.UserName)} ({c.UserLogin}) - {(c.CreatedDate.HasValue ? c.CreatedDate.Value.ToString("dd.MM.yyyy HH:mm") : "-")}",
                        Text = c.Text,
                        DeleteVisibility = (SessionContext.IsAdmin || (SessionContext.CurrentUserId.HasValue && SessionContext.CurrentUserId.Value == c.UserId))
                            ? Visibility.Visible
                            : Visibility.Collapsed
                    }).ToList();

                    CommentsListBox.ItemsSource = items;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки комментариев: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new AddArticleWindow(_article);
            editWindow.Owner = this;
            
            if (editWindow.ShowDialog() == true)
            {
                // Перезагружаем статью после редактирования
                LoadArticle();
            }
        }

        private void AddTestButton_Click(object sender, RoutedEventArgs e)
        {
            int? existingTestId;
            using (var context = new DBEntities())
            {
                existingTestId = context.Tests
                    .Where(t => t.ArticleId == _articleId)
                    .OrderBy(t => t.Id)
                    .Select(t => (int?)t.Id)
                    .FirstOrDefault();
            }

            var testWindow = new AddTestWindow(_articleId, existingTestId);
            testWindow.Owner = this;
            testWindow.ShowDialog();
        }

        private void PassTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionContext.CurrentUserId.HasValue)
            {
                MessageBox.Show("Не удалось определить текущего пользователя.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = new DBEntities())
            {
                var testId = context.Tests
                    .Where(t => t.ArticleId == _articleId && (t.IsActive ?? true))
                    .OrderBy(t => t.Id)
                    .Select(t => (int?)t.Id)
                    .FirstOrDefault();

                if (!testId.HasValue)
                {
                    MessageBox.Show("Для этой статьи пока нет доступного теста.", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var takeTestWindow = new TakeTestWindow(testId.Value, SessionContext.CurrentUserId.Value);
                takeTestWindow.Owner = this;
                takeTestWindow.ShowDialog();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить эту статью?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new DBEntities())
                    {
                        DeleteArticleWithDependencies(context, _articleId);
                        context.SaveChanges();
                    }

                    MessageBox.Show("Статья успешно удалена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении статьи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static void DeleteArticleWithDependencies(DBEntities context, int articleId)
        {
            var testIds = context.Tests
                .Where(t => t.ArticleId == articleId)
                .Select(t => t.Id)
                .ToList();

            if (testIds.Count > 0)
            {
                var questionIds = context.Questions
                    .Where(q => testIds.Contains(q.TestId))
                    .Select(q => q.Id)
                    .ToList();

                if (questionIds.Count > 0)
                {
                    var answers = context.Answers.Where(a => questionIds.Contains(a.QuestionId)).ToList();
                    if (answers.Count > 0)
                        context.Answers.RemoveRange(answers);

                    var questions = context.Questions.Where(q => questionIds.Contains(q.Id)).ToList();
                    if (questions.Count > 0)
                        context.Questions.RemoveRange(questions);
                }

                var selectedTests = context.SelectedTests.Where(s => testIds.Contains(s.TestId)).ToList();
                if (selectedTests.Count > 0)
                    context.SelectedTests.RemoveRange(selectedTests);

                var testResults = context.TestResults.Where(r => testIds.Contains(r.TestId)).ToList();
                if (testResults.Count > 0)
                    context.TestResults.RemoveRange(testResults);

                var tests = context.Tests.Where(t => testIds.Contains(t.Id)).ToList();
                if (tests.Count > 0)
                    context.Tests.RemoveRange(tests);
            }

            var comments = context.ArticleComments.Where(c => c.ArticleId == articleId).ToList();
            if (comments.Count > 0)
                context.ArticleComments.RemoveRange(comments);

            var article = context.Articles.Find(articleId);
            if (article != null)
                context.Articles.Remove(article);
        }

        private void SendCommentButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionContext.CurrentUserId.HasValue)
            {
                MessageBox.Show("Не удалось определить пользователя.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var commentText = NewCommentTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(commentText))
            {
                MessageBox.Show("Введите текст комментария.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new DBEntities())
                {
                    context.ArticleComments.Add(new ArticleComments
                    {
                        ArticleId = _articleId,
                        UserId = SessionContext.CurrentUserId.Value,
                        Text = commentText,
                        CreatedDate = DateTime.Now
                    });
                    context.SaveChanges();
                }

                NewCommentTextBox.Clear();
                LoadComments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения комментария: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCommentButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is int commentId))
                return;

            var result = MessageBox.Show("Удалить этот комментарий?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var context = new DBEntities())
                {
                    var comment = context.ArticleComments.FirstOrDefault(c => c.Id == commentId && c.ArticleId == _articleId);
                    if (comment == null)
                    {
                        MessageBox.Show("Комментарий не найден.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadComments();
                        return;
                    }

                    var canDelete = SessionContext.IsAdmin
                        || (SessionContext.CurrentUserId.HasValue && SessionContext.CurrentUserId.Value == comment.UserId);

                    if (!canDelete)
                    {
                        MessageBox.Show("Удалять можно только свои комментарии.", "Доступ запрещен",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    context.ArticleComments.Remove(comment);
                    context.SaveChanges();
                }

                LoadComments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления комментария: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderArticleContent(string content)
        {
            ContentPanel.Children.Clear();

            if (string.IsNullOrWhiteSpace(content))
                return;

            var imageRegex = new Regex(@"!\[[^\]]*\]\((?<path>[^)]+)\)", RegexOptions.IgnoreCase);
            var currentIndex = 0;

            foreach (Match match in imageRegex.Matches(content))
            {
                AddTextBlock(content.Substring(currentIndex, match.Index - currentIndex));
                AddInlineImage(match.Groups["path"].Value);
                currentIndex = match.Index + match.Length;
            }

            AddTextBlock(content.Substring(currentIndex));
        }

        private void AddTextBlock(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            ContentPanel.Children.Add(new TextBlock
            {
                Text = text.Trim(),
                FontSize = 16,
                Foreground = System.Windows.Media.Brushes.WhiteSmoke,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 26,
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        private void AddInlineImage(string rawPath)
        {
            try
            {
                var normalizedPath = Uri.UnescapeDataString(rawPath.Trim());
                var fileName = Path.GetFileName(normalizedPath);
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArticleMedia", fileName);

                if (!File.Exists(fullPath))
                    return;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var inlineImage = new Image
                {
                    Source = bitmap,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    MaxHeight = 380
                };

                ContentPanel.Children.Add(new Border
                {
                    Margin = new Thickness(0, 4, 0, 14),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 57, 72)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Child = inlineImage
                });
            }
            catch
            {
                // Пропускаем поврежденные ссылки на изображения в контенте.
            }
        }

        private sealed class CommentItem
        {
            public int CommentId { get; set; }
            public string Header { get; set; }
            public string Text { get; set; }
            public Visibility DeleteVisibility { get; set; }
        }
    }
}
