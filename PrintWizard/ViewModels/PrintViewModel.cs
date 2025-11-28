using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PrintWizard.Common;
using PrintWizard.Models;

namespace PrintWizard.ViewModels
{
    public class PrintViewModel : INotifyPropertyChanged
    {
        private const double MmToDIPsFactor = 96.0 / 25.4;

        Canvas canvas;
        public PrintViewModel(Canvas canvas) : this()
        {
            this.canvas = canvas;
        }
        public PrintViewModel()
        {
            PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
            ResetPositionCommand = new RelayCommand(ExecuteResetPosition);
            AddTextItemCommand = new RelayCommand(ExecuteAddTextItem);
            AddQrCodeItemCommand = new RelayCommand(ExecuteAddQrCodeItem);

            InitializePaperSizes();
            InitializeMargins();
            LoadPrinters();

            // 核心修改：默认选择 60x40mm 的标签纸，以获得更大的默认预览比例
            SelectedPaperSize = PaperSizes.FirstOrDefault(p => p.Name.Contains("60×40mm")) ?? PaperSizes.FirstOrDefault();

            SelectedMargin = Margins.FirstOrDefault(m => m.DisplayName.Contains("极窄边距")) ?? Margins.FirstOrDefault();

            UpdatePrintArea();
            StatusMessage = "就绪 - 请添加内容";

            // [新增] 初始化导出 CPCL 命令
            ExportCpclCommand = new RelayCommand(ExecuteExportCpcl, CanExecutePrint);
        }

        // 2. [新增] 导出命令属性
        public ICommand ExportCpclCommand { get; }

        public ICommand PrintCommand { get; }
        public ICommand ResetPositionCommand { get; }
        public ICommand AddTextItemCommand { get; }
        public ICommand AddQrCodeItemCommand { get; }

        private string newItemText = "新的文本内容";
        public string NewItemText
        {
            get => newItemText;
            set { newItemText = value; OnPropertyChanged(); }
        }

        private string newQrCodeContent = "53454456434553";
        public string NewQrCodeContent
        {
            get => newQrCodeContent;
            set { newQrCodeContent = value; OnPropertyChanged(); }
        }

        private double newItemFontSize = 14;
        public double NewItemFontSize
        {
            get => newItemFontSize;
            set { newItemFontSize = Math.Round(value); OnPropertyChanged(); } // 保证字号是整数
        }

        private bool newItemIsBold = false;
        public bool NewItemIsBold
        {
            get => newItemIsBold;
            set { newItemIsBold = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PrintItemBase> PrintItems { get; } = new ObservableCollection<PrintItemBase>();

        private string statusMessage;
        public string StatusMessage
        {
            get => statusMessage;
            set { statusMessage = value; OnPropertyChanged(); }
        }

        // 添加文本项 
        private void ExecuteAddTextItem(object parameter)
        {
            var newItem = new TextPrintItem
            {
                Content = NewItemText,
                X = 10, // 相对 Canvas (可打印区域) 的位置
                Y = 10 + PrintItems.Count * 10, // 相对 Canvas (可打印区域) 的位置
                FontSize = NewItemFontSize,
                IsBold = NewItemIsBold
            };
            PrintItems.Add(newItem);
            StatusMessage = $"已添加文本项 (字号:{NewItemFontSize}, 加粗:{NewItemIsBold})";
        }

        // 添加二维码项 
        private void ExecuteAddQrCodeItem(object parameter)
        {
            var newItem = new QrCodePrintItem
            {
                QrContent = NewQrCodeContent,
                X = 50 + PrintItems.Count * 10,
                Y = 50
            };
            PrintItems.Add(newItem);
            StatusMessage = $"已添加二维码项";
        }

        // 重置位置 
        private void ExecuteResetPosition(object parameter)
        {
            int i = 0;
            double startX = 10;
            double startY = 10;
            foreach (var item in PrintItems)
            {
                item.X = startX;
                item.Y = startY + i * 100;
                i++;
            }
            StatusMessage = "位置已重置";
        }

        private bool CanExecutePrint(object parameter) => PrintItems.Any();

        private void ExecutePrint(object parameter)
        {
            try
            {
                if (!PrintItems.Any()) return;

                var printDialog = new PrintDialog();
                if (SelectedPrinter != null) printDialog.PrintQueue = SelectedPrinter;
                printDialog.PrintTicket.CopyCount = Copies;

                // 配置纸张尺寸
                double paperWidth = SelectedPaperSize.Width * MmToDIPsFactor;
                double paperHeight = SelectedPaperSize.Height * MmToDIPsFactor;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(paperWidth, paperHeight);

                var document = CreatePrintDocument();
                if (document == null) return;

                // 打印
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "自定义打印文档");
                StatusMessage = $"打印成功 - {DateTime.Now:HH:mm:ss}";
                string directoryPath = @"E:\" + new Random().NextInt64() + "image.png";
                ConvertCanvasToImage(this.canvas, directoryPath);// 测试保存预览图像
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private FlowDocument CreatePrintDocument()
        {
            try
            {
                double marginInPixels = SelectedMargin.Margin * MmToDIPsFactor;
                double pageWidth = SelectedPaperSize.Width * MmToDIPsFactor;
                double pageHeight = SelectedPaperSize.Height * MmToDIPsFactor;

                var document = new FlowDocument
                {
                    Background = Brushes.White,
                    FontFamily = new FontFamily("Microsoft YaHei"),
                    FontSize = 14,
                    // WYSIWYG 核心：FlowDocument 的 PagePadding 等于选定的页边距
                    PagePadding = new Thickness(marginInPixels),
                    PageWidth = pageWidth,
                    PageHeight = pageHeight,
                    ColumnWidth = double.PositiveInfinity // 禁用多列布局
                };

                // 创建一个 Canvas，其大小就是可打印区域的大小
                var printCanvas = new Canvas
                {
                    Width = PrintAreaWidth,
                    Height = PrintAreaHeight
                };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(PrintItems);
                Console.WriteLine(json);
                foreach (var item in PrintItems)
                {
                    UIElement elementToPrint = null;

                    // WYSIWYG 核心：X/Y 就是最终的相对 Canvas 坐标
                    double relativeX = item.X;
                    double relativeY = item.Y;

                    if (item is TextPrintItem textItem)
                    {
                        var textBlock = new TextBlock(new Run(textItem.Content))
                        {
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = textItem.FontSize,
                            FontWeight = textItem.IsBold ? FontWeights.Bold : FontWeights.Normal,
                            Width = textItem.Width
                        };
                        elementToPrint = textBlock;
                    }
                    else if (item is QrCodePrintItem qrItem)
                    {
                        if (qrItem.QrImageSource is BitmapSource bitmapSource)
                        {
                            elementToPrint = new Image
                            {
                                Source = bitmapSource,
                                Width = qrItem.Width,
                                Height = qrItem.Height,
                                Stretch = Stretch.Uniform
                            };
                        }
                    }

                    if (elementToPrint != null)
                    {
                        // 直接使用 item.X 和 item.Y 设置位置
                        Canvas.SetLeft(elementToPrint, relativeX);
                        Canvas.SetTop(elementToPrint, relativeY);
                        printCanvas.Children.Add(elementToPrint);
                    }
                }

                document.Blocks.Add(new BlockUIContainer(printCanvas));

                return document;
            }
            catch (Exception ex)
            {
                StatusMessage = $"创建文档失败: {ex.Message}";
                return null;
            }
        }


        public void ConvertCanvasToImage(Canvas canvas, string outputPath)
        {
            // 1. 创建 RenderTargetBitmap
            int width = (int)canvas.ActualWidth;
            int height = (int)canvas.ActualHeight;
            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);

            // 2. 渲染 Canvas 到 RenderTargetBitmap
            rtb.Render(canvas);

            // 3. 创建 BitmapEncoder 来保存图像
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            // 4. 将图像保存到指定路径
            using (FileStream stream = new FileStream(outputPath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }


        // 3. [新增] 核心函数：生成 CPCL 并保存
        private void ExecuteExportCpcl(object parameter)
        {
            try
            {
                if (!PrintItems.Any())
                {
                    StatusMessage = "没有可导出的内容";
                    return;
                }

                // === 配置参数 ===
                // CPCL 标准分辨率通常为 203 dpi (8 dots/mm)
                const double dotsPerMm = 8.0;
                // WPF 单位 (DIP) 到 毫米 (mm) 的转换系数 (96 DIP = 25.4 mm)
                const double dipToMm = 25.4 / 96.0;
                // 综合转换系数：DIP -> Dots
                double dipToDots = dipToMm * dotsPerMm;

                // 获取标签高度和宽度 (单位转换：mm -> dots)
                int labelHeightDots = (int)(SelectedPaperSize.Height * dotsPerMm);
                int labelWidthDots = (int)(SelectedPaperSize.Width * dotsPerMm); // 仅用于页面宽度指令
                int quantity = Copies;

                StringBuilder sb = new StringBuilder();

                // === 1. CPCL 头部指令 ===
                // ! 0 200 200 {height} {quantity}
                // 0: 水平偏移, 200: 横向分辨率, 200: 纵向分辨率, height: 标签高度, quantity: 打印数量
                sb.AppendLine($"! 0 200 200 {labelHeightDots} {quantity}");

                // 设置页面宽度 (这对某些打印机很重要，防止打印越界)
                sb.AppendLine($"PAGE-WIDTH {labelWidthDots}");

                // === 2. 遍历内容项 ===
                foreach (var item in PrintItems)
                {
                    // 计算坐标 (DIP -> Dots)
                    // 注意：CPCL 坐标系原点也在左上角，X 向右，Y 向下，与 WPF 一致
                    int x = (int)(item.X * dipToDots);
                    int y = (int)(item.Y * dipToDots);

                    if (item is TextPrintItem textItem)
                    {
                        // === 文本处理 ===
                        // CPCL 文本指令: TEXT {font} {size} {x} {y} {data}
                        // 或者 T {font} {size} {x} {y} {data}
                        // 这里使用字体 "5" (常用内置等宽字体) 或 "7" (大字体)，根据 WPF 字号简单映射

                        int fontId = 7; // 默认使用 7 号字体 (比较通用)
                        int fontSize = 0; // 字号大小参数，部分字体支持放缩

                        // 简单的字号映射逻辑
                        if (textItem.FontSize < 12) { fontId = 5; fontSize = 0; }
                        else if (textItem.FontSize > 20) { fontId = 7; fontSize = 1; } // 伪放大

                        // 处理加粗 (CPCL 使用 SETBOLD)
                        if (textItem.IsBold)
                        {
                            sb.AppendLine("SETBOLD 1");
                        }

                        // 生成文本指令
                        // 注意：CPCL 对中文支持取决于打印机字库。如果打印机不支持中文，这行指令可能打印乱码。
                        // 格式: TEXT {font} {size} {x} {y} {content}
                        sb.AppendLine($"TEXT {fontId} {fontSize} {x} {y} {textItem.Content}");

                        if (textItem.IsBold)
                        {
                            sb.AppendLine("SETBOLD 0"); // 关闭加粗
                        }
                    }
                    else if (item is QrCodePrintItem qrItem)
                    {
                        // === 二维码处理 ===
                        // CPCL 二维码指令 (垂直和水平放置的 QR Code)
                        // BARCODE QR {x} {y} M 2 U 6
                        // {data}
                        // ENDQR

                        sb.AppendLine($"BARCODE QR {x} {y} M 2 U 6");
                        sb.AppendLine("MA," + qrItem.QrContent); // MA, 是某些固件要求的起始符，或者是直接放内容
                        sb.AppendLine("ENDQR");
                    }
                }

                // 触发表单打印
                sb.AppendLine("PRINT");

                // === 4. 保存文件 ===
                string content = sb.ToString();
                string fileName = $"CPCL_{DateTime.Now:yyyyMMdd}_{new Random().Next(1000, 9999)}.txt";
                string filePath = Path.Combine(@"E:\", fileName);

                // 确保 E 盘存在，否则回退到 C 盘根目录或临时目录防止崩溃
                if (!Directory.Exists(@"E:\"))
                {
                    filePath = Path.Combine(Path.GetTempPath(), fileName);
                    StatusMessage = $"E盘不存在，保存至: {filePath}";
                }

                // 使用 GB2312 编码写入，因为大多数国内 CPCL 打印机处理中文需要 GBK/GB2312
                // .NET Core/5+ 可能需要注册 CodePagesEncodingProvider，如果乱码请改用 Default 或 UTF8
                Encoding encoding = Encoding.Default;
                try { encoding = Encoding.GetEncoding("GB2312"); } catch { }

                File.WriteAllText(filePath, content, encoding);

                StatusMessage = $"CPCL 指令已导出: {filePath}";
                MessageBox.Show($"文件已保存至:\n{filePath}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出 CPCL 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Standard Properties

        private int copies = 1;
        public int Copies
        {
            get => copies;
            set { if (value > 0 && value <= 100) { copies = value; OnPropertyChanged(nameof(Copies)); } }
        }

        public ObservableCollection<PaperSize> PaperSizes { get; } = new();
        public ObservableCollection<MarginSetting> Margins { get; } = new();
        public ObservableCollection<PrintQueue> AvailablePrinters { get; } = new();
        public PrintQueue SelectedPrinter { get; set; }

        private double printAreaWidth;
        public double PrintAreaWidth
        {
            get => printAreaWidth;
            set { printAreaWidth = value; OnPropertyChanged(); OnPropertyChanged(nameof(PrintAreaSizeInfo)); }
        }

        private double printAreaHeight;
        public double PrintAreaHeight
        {
            get => printAreaHeight;
            set { printAreaHeight = value; OnPropertyChanged(); OnPropertyChanged(nameof(PrintAreaSizeInfo)); }
        }

        private double printMargin;
        // PrintMargin 设定 Border 的 Padding (可视化)
        public double PrintMargin
        {
            get => printMargin;
            set { printMargin = value; OnPropertyChanged(); }
        }

        public string PrintAreaSizeInfo => $"可打印区域: {PrintAreaWidth:F0}x{PrintAreaHeight:F0} DIPs";

        private PaperSize selectedPaperSize;
        public PaperSize SelectedPaperSize
        {
            get => selectedPaperSize;
            set
            {
                selectedPaperSize = value;
                OnPropertyChanged();
                UpdatePrintArea();
            }
        }

        private MarginSetting selectedMargin;
        public MarginSetting SelectedMargin
        {
            get => selectedMargin;
            set
            {
                selectedMargin = value;
                OnPropertyChanged();
                UpdatePrintArea();
            }
        }

        private void UpdatePrintArea()
        {
            double paperWidthMm = SelectedPaperSize?.Width ?? 210;
            double paperHeightMm = SelectedPaperSize?.Height ?? 297;
            double marginMm = SelectedMargin?.Margin ?? 20;

            // PrintMargin 设定 Border 的 Padding (可视化)
            PrintMargin = marginMm * MmToDIPsFactor;

            double printableWidthMm = paperWidthMm - 2 * marginMm;
            double printableHeightMm = paperHeightMm - 2 * marginMm;

            if (printableWidthMm < 0) printableWidthMm = 0;
            if (printableHeightMm < 0) printableHeightMm = 0;

            // PrintAreaWidth/Height 设定 Canvas 的尺寸
            PrintAreaWidth = printableWidthMm * MmToDIPsFactor;
            PrintAreaHeight = printableHeightMm * MmToDIPsFactor;
        }

        private void InitializePaperSizes()
        {
            PaperSizes.Add(new PaperSize("60×40mm (标签)", 60, 40));
            PaperSizes.Add(new PaperSize("A4 (210×297mm)", 210, 297));
            PaperSizes.Add(new PaperSize("B5 (176×250mm)", 176, 250));
        }

        private void InitializeMargins()
        {
            Margins.Add(new MarginSetting("无边距 (0mm)", 0));
            Margins.Add(new MarginSetting("极窄边距 (2mm)", 2));
            Margins.Add(new MarginSetting("标准边距 (20mm)", 20));
        }

        private void LoadPrinters()
        {
            try
            {
                var printServer = new LocalPrintServer();
                foreach (var queue in printServer.GetPrintQueues())
                {
                    AvailablePrinters.Add(queue);
                }
                SelectedPrinter = printServer.DefaultPrintQueue;
            }
            catch { }
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}