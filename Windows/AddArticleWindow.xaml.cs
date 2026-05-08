using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace VerhozinaIvanovDiplom.Windows
{
    /// <summary>
    /// Логика взаимодействия для AddArticleWindow.xaml
    /// </summary>
    public partial class AddArticleWindow : Window
    {
        private sealed class CarPartOption
        {
            public int? Id { get; set; }
            public string DisplayName { get; set; }
        }

        private string selectedImagePath;
        private Articles articleToEdit;

        public AddArticleWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            SaveButton.IsDefault = true;
            EditButton.IsDefault = false;
        }

        public AddArticleWindow(Articles article)
        {
            InitializeComponent();
            articleToEdit = article;
            Loaded += OnWindowLoaded;

            // Заполняем поля существующими данными
            TitleTextBox.Text = article.Title;
            ContentTextBox.Text = article.Content;

            // Загружаем изображение, если оно есть
            if (article.ImageData != null && article.ImageData.Length > 0)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(article.ImageData);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImagePreview.Source = bitmap;
                    ImagePreviewBorder.Visibility = Visibility.Visible;
                    ImagePathTextBlock.Text = "Изображение загружено из базы данных";
                }
                catch
                {
                    ImagePathTextBlock.Text = "Ошибка загрузки изображения";
                }
            }

            // Показываем кнопки редактирования и удаления
            EditButton.Visibility = Visibility.Visible;
            DeleteButton.Visibility = Visibility.Visible;
            SaveButton.Visibility = Visibility.Collapsed;
            EditButton.IsDefault = true;
            SaveButton.IsDefault = false;

            Title = "Редактирование статьи";
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnWindowLoaded;
            PopulateCarPartCombo();
        }

        private void PopulateCarPartCombo()
        {
            CarPartComboBox.Items.Clear();
            CarPartComboBox.DisplayMemberPath = "DisplayName";

            try
            {
                using (var context = new DBEntities())
                {
                    CarPartComboBox.Items.Add(new CarPartOption { Id = null, DisplayName = "Не выбрано" });
                    foreach (var part in context.CarParts.OrderBy(p => p.Id))
                        CarPartComboBox.Items.Add(new CarPartOption { Id = part.Id, DisplayName = part.Name });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить части автомобиля. Выполните скрипт CarParts.sql в базе данных.\n\n{ex.Message}",
                    "База данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                CarPartComboBox.Items.Add(new CarPartOption { Id = null, DisplayName = "Не выбрано" });
            }

            if (articleToEdit?.CarPartId is int selectedId)
            {
                foreach (CarPartOption opt in CarPartComboBox.Items)
                {
                    if (opt.Id == selectedId)
                    {
                        CarPartComboBox.SelectedItem = opt;
                        return;
                    }
                }
            }

            CarPartComboBox.SelectedIndex = 0;
        }

        private int? GetSelectedCarPartId()
        {
            return (CarPartComboBox.SelectedItem as CarPartOption)?.Id;
        }

        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*",
                Title = "Выберите изображение для статьи"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedImagePath = openFileDialog.FileName;
                ImagePathTextBlock.Text = Path.GetFileName(selectedImagePath);
                
                // Отображение превью изображения
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(selectedImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    
                    ImagePreview.Source = bitmap;
                    ImagePreviewBorder.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InsertInlineImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*",
                Title = "Выберите изображение для вставки в статью"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            try
            {
                var markup = SaveInlineImageAndGetMarkup(openFileDialog.FileName);
                var insertionText = Environment.NewLine + markup + Environment.NewLine;
                var caretIndex = ContentTextBox.CaretIndex;

                ContentTextBox.Text = ContentTextBox.Text.Insert(caretIndex, insertionText);
                ContentTextBox.CaretIndex = caretIndex + insertionText.Length;
                ContentTextBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вставки изображения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string SaveInlineImageAndGetMarkup(string sourceFilePath)
        {
            var mediaDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArticleMedia");
            Directory.CreateDirectory(mediaDirectory);

            var extension = Path.GetExtension(sourceFilePath);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".png";

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var targetPath = Path.Combine(mediaDirectory, fileName);
            File.Copy(sourceFilePath, targetPath, true);

            var escapedFileName = Uri.EscapeDataString(fileName);
            return $"![image](media/{escapedFileName})";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название статьи.", "Валидация", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите текст статьи.", "Валидация", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ContentTextBox.Focus();
                return;
            }

            try
            {
                byte[] imageBytes = null;

                // Чтение изображения в байтовый массив
                if (!string.IsNullOrEmpty(selectedImagePath))
                {
                    imageBytes = File.ReadAllBytes(selectedImagePath);
                }

                // Создание новой статьи
                var article = new Articles
                {
                    Title = TitleTextBox.Text.Trim(),
                    Content = ContentTextBox.Text.Trim(),
                    PublishedDate = DateTime.Now,
                    IsPublished = true,
                    ImageData = imageBytes,
                    CarPartId = GetSelectedCarPartId()
                };

                // Сохранение в базу данных
                using (var context = new DBEntities())
                {
                    context.Articles.Add(article);
                    context.SaveChanges();
                }

                MessageBox.Show("Статья успешно добавлена!", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении статьи: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название статьи.", "Валидация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ContentTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите текст статьи.", "Валидация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ContentTextBox.Focus();
                return;
            }

            try
            {
                // Обновляем статью
                articleToEdit.Title = TitleTextBox.Text.Trim();
                articleToEdit.Content = ContentTextBox.Text.Trim();
                articleToEdit.CarPartId = GetSelectedCarPartId();

                // Обновляем изображение, если выбрано новое
                if (!string.IsNullOrEmpty(selectedImagePath))
                {
                    articleToEdit.ImageData = File.ReadAllBytes(selectedImagePath);
                }

                // Сохраняем изменения в базе данных
                using (var context = new DBEntities())
                {
                    context.Entry(articleToEdit).State = System.Data.Entity.EntityState.Modified;
                    context.SaveChanges();
                }

                MessageBox.Show("Статья успешно обновлена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статьи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                        DeleteArticleWithDependencies(context, articleToEdit.Id);
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
    }
}
