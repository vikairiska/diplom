using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VerhozinaIvanovDiplom
{
    /// <summary>
    /// Рендер текста статьи: заголовки (# ## ###), списки, абзацы, встроенные изображения.
    /// </summary>
    public static class ArticleRichContentRenderer
    {
        private static readonly Brush BodyBrush = new SolidColorBrush(Color.FromRgb(216, 228, 240));
        private static readonly Brush HeadingBrush = new SolidColorBrush(Color.FromRgb(244, 248, 253));
        private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(174, 190, 210));

        private static readonly Regex ImageToken = new Regex(@"!\[[^\]]*\]\((?<path>[^)]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HeadingOpen = new Regex(@"^(#{1,3})(?!#)\s*(.+)$", RegexOptions.Compiled);
        private static readonly Regex HeadingCloseTrim = new Regex(@"\s*#+\s*$", RegexOptions.Compiled);
        private static readonly Regex BulletLine = new Regex(@"^[\-\*•]\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex OrderedLine = new Regex(@"^(\d+)\.\s+(.+)$", RegexOptions.Compiled);

        static ArticleRichContentRenderer()
        {
            BodyBrush.Freeze();
            HeadingBrush.Freeze();
            MutedBrush.Freeze();
        }

        public static void Render(Panel panel, string content)
        {
            if (panel == null)
                return;

            panel.Children.Clear();

            if (string.IsNullOrWhiteSpace(content))
                return;

            var currentIndex = 0;
            foreach (Match match in ImageToken.Matches(content))
            {
                var before = content.Substring(currentIndex, match.Index - currentIndex);
                RenderTextSegment(panel, before);
                AddInlineImage(panel, match.Groups["path"].Value);
                currentIndex = match.Index + match.Length;
            }

            RenderTextSegment(panel, content.Substring(currentIndex));
        }

        /// <summary>
        /// Заголовок: «# текст», «## текст» (пробел после # необязателен),
        /// либо с закрывающими решётками: «#текст##», «###текст###».
        /// </summary>
        private static bool TryParseHeadingLine(string trimmed, out int level, out string title)
        {
            level = 0;
            title = null;

            var m = HeadingOpen.Match(trimmed);
            if (!m.Success)
                return false;

            level = m.Groups[1].Length;
            var body = m.Groups[2].Value.Trim();
            body = HeadingCloseTrim.Replace(body, "").Trim();
            if (string.IsNullOrWhiteSpace(body))
                return false;

            title = body;
            return true;
        }

        private static void RenderTextSegment(Panel panel, string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
                return;

            var lines = segment.Replace("\r\n", "\n").Split('\n');
            var paragraph = new List<string>();

            void FlushParagraph()
            {
                if (paragraph.Count == 0)
                    return;

                var text = string.Join(Environment.NewLine, paragraph.Select(s => s.TrimEnd())).Trim();
                paragraph.Clear();
                if (string.IsNullOrEmpty(text))
                    return;

                panel.Children.Add(CreateBodyParagraph(text));
            }

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed))
                {
                    FlushParagraph();
                    continue;
                }

                if (TryParseHeadingLine(trimmed, out var hLevel, out var hTitle))
                {
                    FlushParagraph();
                    panel.Children.Add(CreateHeading(hLevel, hTitle));
                    continue;
                }

                var bullet = BulletLine.Match(trimmed);
                if (bullet.Success)
                {
                    FlushParagraph();
                    panel.Children.Add(CreateBulletLine(bullet.Groups[1].Value.Trim()));
                    continue;
                }

                var ord = OrderedLine.Match(trimmed);
                if (ord.Success)
                {
                    FlushParagraph();
                    if (int.TryParse(ord.Groups[1].Value, out var n))
                        panel.Children.Add(CreateOrderedLine(n, ord.Groups[2].Value.Trim()));
                    else
                        panel.Children.Add(CreateOrderedLine(1, ord.Groups[2].Value.Trim()));
                    continue;
                }

                paragraph.Add(trimmed);
            }

            FlushParagraph();
        }

        private static TextBlock CreateBodyParagraph(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 17,
                Foreground = BodyBrush,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 29,
                Margin = new Thickness(0, 0, 0, 16),
                FontFamily = new FontFamily("Segoe UI")
            };
        }

        private static TextBlock CreateHeading(int level, string text)
        {
            double size = level == 1 ? 30 : level == 2 ? 24 : 20;
            var marginTop = level == 1 ? 8.0 : 18.0;

            return new TextBlock
            {
                Text = text,
                FontSize = size,
                FontWeight = FontWeights.SemiBold,
                Foreground = HeadingBrush,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = size + 10,
                Margin = new Thickness(0, marginTop, 0, 10),
                FontFamily = new FontFamily("Segoe UI")
            };
        }

        private static TextBlock CreateBulletLine(string text)
        {
            return new TextBlock
            {
                Text = "•  " + text,
                FontSize = 17,
                Foreground = BodyBrush,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 28,
                Margin = new Thickness(18, 2, 0, 6),
                FontFamily = new FontFamily("Segoe UI")
            };
        }

        private static TextBlock CreateOrderedLine(int index, string text)
        {
            return new TextBlock
            {
                Text = $"{index}. {text}",
                FontSize = 17,
                Foreground = BodyBrush,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 28,
                Margin = new Thickness(18, 2, 0, 6),
                FontFamily = new FontFamily("Segoe UI")
            };
        }

        private static void AddInlineImage(Panel panel, string rawPath)
        {
            try
            {
                var normalizedPath = Uri.UnescapeDataString(rawPath.Trim());
                var fileName = Path.GetFileName(normalizedPath);
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArticleMedia", fileName);

                if (!File.Exists(fullPath))
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = "(изображение не найдено)",
                        FontSize = 14,
                        Foreground = MutedBrush,
                        FontStyle = FontStyles.Italic,
                        Margin = new Thickness(0, 4, 0, 12)
                    });
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var inlineImage = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    MaxHeight = 380
                };

                panel.Children.Add(new Border
                {
                    Margin = new Thickness(0, 8, 0, 20),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(42, 57, 72)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Child = inlineImage
                });
            }
            catch
            {
                // пропускаем битые ссылки
            }
        }
    }
}
