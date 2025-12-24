using PrintWizard.Common;
using PrintWizard.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrintWizard.Service
{
    /// <summary>
    /// 画布服务
    /// </summary>
    public class CanvasService
    {
        public Canvas CreateCleanPrintCanvas(IEnumerable<PrintItemBase> items, PaperSize size, MarginSetting marginSetting)
        {
            double scale = PrintUtils.MmToDipFactor;
            double paperW = size.Width * scale;
            double paperH = size.Height * scale;
            double margin = marginSetting.Margin * scale;

            Canvas cleanCv = new Canvas
            {
                Width = paperW,
                Height = paperH,
                Background = Brushes.White
            };

            foreach (var item in items)
            {
                FrameworkElement visualElement = null;
                double finalX = item.X + margin;
                double finalY = item.Y + margin;

                if (item is TextPrintItem t)
                {
                    visualElement = new TextBlock
                    {
                        Text = t.Content,
                        FontSize = t.FontSize,
                        FontWeight = t.IsBold ? FontWeights.Bold : FontWeights.Normal,
                        Width = t.Width,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.Black,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                }
                else if (item is QrCodePrintItem q)
                {
                    visualElement = new Image
                    {
                        Source = q.QrImageSource,
                        Width = q.Width,
                        Height = q.Height,
                        Stretch = Stretch.Fill
                    };
                }

                if (visualElement != null)
                {
                    Canvas.SetLeft(visualElement, finalX);
                    Canvas.SetTop(visualElement, finalY);
                    cleanCv.Children.Add(visualElement);
                }
            }

            // 强制布局更新
            Size s = new Size(paperW, paperH);
            cleanCv.Measure(s);
            cleanCv.Arrange(new Rect(new Point(0, 0), s));
            cleanCv.UpdateLayout();

            return cleanCv;
        }

        public void SaveCanvasToImage(Canvas canvas, string outputPath)
        {
            int w = (int)canvas.ActualWidth;
            int h = (int)canvas.ActualHeight;
            if (w <= 0 || h <= 0) return;

            RenderTargetBitmap rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(canvas);

            PngBitmapEncoder enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));

            using (Stream s = File.Create(outputPath))
            {
                enc.Save(s);
            }
        }
    }
}
