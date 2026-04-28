using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace VerhozinaIvanovDiplom.Windows
{
    /// <summary>
    /// Логика взаимодействия для AddArticleWindow.xaml
    /// </summary>
    public partial class AddArticleWindow : Window
    {
        private string selectedImagePath;
        private Articles articleToEdit;

        public AddArticleWindow()
        {
            InitializeComponent();
        }

        public AddArticleWindow(Articles article)
        {
            InitializeComponent();
            articleToEdit = article;

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

            Title = "Редактирование статьи";
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
                    ImageData = imageBytes
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
                        var article = context.Articles.Find(articleToEdit.Id);
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
    }
}
