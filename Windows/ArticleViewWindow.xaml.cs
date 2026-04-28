using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

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
                        TitleTextBlock.Text = article.Title;
                        ContentTextBlock.Text = article.Content;

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
                                           UserName = user.FullName,
                                           UserLogin = user.Login,
                                           comment.Text,
                                           comment.CreatedDate
                                       }).ToList();

                    var items = rawComments.Select(c => new CommentItem
                    {
                        Header = $"{(string.IsNullOrWhiteSpace(c.UserName) ? c.UserLogin : c.UserName)} ({c.UserLogin}) - {(c.CreatedDate.HasValue ? c.CreatedDate.Value.ToString("dd.MM.yyyy HH:mm") : "-")}",
                        Text = c.Text
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
                        var article = context.Articles.Find(_articleId);
                        if (article != null)
                        {
                            context.Articles.Remove(article);
                            context.SaveChanges();
                        }
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

        private sealed class CommentItem
        {
            public string Header { get; set; }
            public string Text { get; set; }
        }
    }
}
