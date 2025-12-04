using Newtonsoft.Json;
using PrintWizard.Models;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrintWizard.Common
{
    public class PrintConfigTool
    {
        /// <summary>
        /// 从配置文件自动执行打印
        /// </summary>
        /// <param name="configFilePath">配置文件路径，默认是 print_config.json</param>
        public static void ExecutePrintFromConfig(string configFilePath)
        {
            string fullPath = Path.Combine(configFilePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("找不到打印配置文件", fullPath);
            }

            // 1. 加载配置
            string json = File.ReadAllText(fullPath);
            PrintConfig config = JsonConvert.DeserializeObject<PrintConfig>(json);

            // 2. 获取打印机
            LocalPrintServer server = new LocalPrintServer();

            // 3. 构建打印画布 (复用逻辑)
            Canvas canvasToPrint = ReconstructCanvas(config);

            // 4. 执行打印
            PrintDialog pd = new PrintDialog();
            pd.PrintQueue = server.DefaultPrintQueue;

            // 处理份数 (WPF PrintDialog 不直接支持份数参数，通常需要循环发送或配置 Ticket，这里简单循环)
            for (int i = 0; i < config.Copies; i++)
            {
                pd.PrintVisual(canvasToPrint, $"Auto Print Job {i + 1}");
            }
        }

        private static Canvas ReconstructCanvas(PrintConfig config)
        {
            // 毫米转像素 (96 DPI)
            double scale = 96.0 / 25.4;
            double paperW = config.PaperWidth * scale;
            double paperH = config.PaperHeight * scale;
            double margin = config.Margin * scale;

            Canvas cleanCv = new Canvas
            {
                Width = paperW,
                Height = paperH,
                Background = Brushes.White
            };

            foreach (var itemDto in config.Items)
            {
                FrameworkElement visualElement = null;
                double finalX = itemDto.X + margin;
                double finalY = itemDto.Y + margin;

                if (itemDto.ItemType == "Text")
                {
                    var tb = new TextBlock
                    {
                        Text = itemDto.Content,
                        FontSize = itemDto.FontSize,
                        FontWeight = itemDto.IsBold ? FontWeights.Bold : FontWeights.Normal,
                        Width = itemDto.Width, // 使用保存的宽度
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.Black,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    visualElement = tb;
                }
                else if (itemDto.ItemType == "QrCode")
                {
                    // 利用现有的 QrCodePrintItem 模型逻辑来生成图片
                    var qrItem = new QrCodePrintItem
                    {
                        QrContent = itemDto.Content,
                        Width = itemDto.Width,
                        Height = itemDto.Height
                    };

                    var img = new Image
                    {
                        Source = qrItem.QrImageSource, // 这里会自动触发生成二维码图片
                        Width = itemDto.Width,
                        Height = itemDto.Height,
                        Stretch = Stretch.Fill
                    };
                    visualElement = img;
                }

                if (visualElement != null)
                {
                    Canvas.SetLeft(visualElement, finalX);
                    Canvas.SetTop(visualElement, finalY);
                    cleanCv.Children.Add(visualElement);
                }
            }

            // 强制重新测量和排列，确保渲染正确
            Size size = new Size(paperW, paperH);
            cleanCv.Measure(size);
            cleanCv.Arrange(new Rect(new Point(0, 0), size));
            cleanCv.UpdateLayout();

            return cleanCv;
        }
    }
}
