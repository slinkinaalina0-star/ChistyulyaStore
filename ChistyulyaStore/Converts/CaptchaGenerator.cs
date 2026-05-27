using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChistyulyaStore.Converts
{
    public static class CaptchaGenerator
    {
        public static DrawingImage Generate(string text)
        {
            DrawingVisual drawingVisual = new DrawingVisual();

            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                Random r = new Random();

                for (int i = 0; i < text.Length; i++)
                {
                    // Каждый символ на своей позиции со случайным сдвигом
                    double x = 10 + i * 25 + r.Next(-5, 5);
                    double y = 10 + r.Next(-5, 5);

                    FormattedText formattedText = new FormattedText(
                        text[i].ToString(),
                        CultureInfo.GetCultureInfo("ru-RU"),
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        24,
                        Brushes.Black,
                        96);

                    drawingContext.DrawText(formattedText, new Point(x, y));

                    // Перечеркивание линии (для каждого символа)
                    drawingContext.DrawLine(
                        new Pen(Brushes.Red, 1.5),
                        new Point(x, y + 10),
                        new Point(x + 20, y + 5));
                }

                // Графический шум (случайные точки)
                for (int i = 0; i < 80; i++)
                {
                    drawingContext.DrawRectangle(
                        Brushes.Gray,
                        null,
                        new Rect(r.Next(0, 130), r.Next(0, 45), 1, 1));
                }
            }

            return new DrawingImage(drawingVisual.Drawing);
        }
    }
}