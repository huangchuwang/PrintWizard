using Microsoft.Win32;
using Newtonsoft.Json;
using PrintWizard.Common;
using PrintWizard.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrintWizard.ViewModels
{
    public class PrintViewModel : INotifyPropertyChanged
    {
        private Canvas _uiCanvas;

        public PrintViewModel(Canvas canvas) : this()
        {
            _uiCanvas = canvas;
        }

        public PrintViewModel()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
            ResetPositionCommand = new RelayCommand(ExecuteResetPosition);
            AddTextItemCommand = new RelayCommand(ExecuteAddTextItem);
            AddQrCodeItemCommand = new RelayCommand(ExecuteAddQrCodeItem);
            ExportCpclCommand = new RelayCommand(ExecuteExportCpcl, CanExecutePrint);
            ImportCpclCommand = new RelayCommand(ExecuteImportCpcl);
            RemoveItemCommand = new RelayCommand(ExecuteRemoveItem);

            ExportConfigCommand = new RelayCommand(ExecuteExportConfig, CanExecutePrint);
            ImportConfigCommand = new RelayCommand(ExecuteImportConfig);

            InitializePaperSizes();
            InitializeMargins();
            LoadPrinters();

            SelectedPaperSize = PaperSizes.FirstOrDefault(p => p.Name.Contains("60×40mm")) ?? PaperSizes.FirstOrDefault();
            SelectedMargin = Margins.FirstOrDefault(m => m.DisplayName.Contains("极窄边距")) ?? Margins.FirstOrDefault();

            StatusMessage = "系统就绪";
        }

        #region Commands & Properties
        public ICommand ExportCpclCommand { get; }
        public ICommand ImportCpclCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ResetPositionCommand { get; }
        public ICommand AddTextItemCommand { get; }
        public ICommand AddQrCodeItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand ExportConfigCommand { get; }
        public ICommand ImportConfigCommand { get; }

        public ObservableCollection<PrintItemBase> PrintItems { get; } = new ObservableCollection<PrintItemBase>();
        public ObservableCollection<PaperSize> PaperSizes { get; } = new();
        public ObservableCollection<MarginSetting> Margins { get; } = new();
        public ObservableCollection<PrintQueue> AvailablePrinters { get; } = new();

        // 【修复】改为完整属性以支持 UI 通知
        private PrintQueue _selectedPrinter;
        public PrintQueue SelectedPrinter
        {
            get => _selectedPrinter;
            set { _selectedPrinter = value; OnPropertyChanged(); }
        }

        private string _newItemText = "请输入文本";
        public string NewItemText { get => _newItemText; set { _newItemText = value; OnPropertyChanged(); } }

        private string _newQrCodeContent = "123456789";
        public string NewQrCodeContent { get => _newQrCodeContent; set { _newQrCodeContent = value; OnPropertyChanged(); } }

        private double _newItemFontSize = 12;
        public double NewItemFontSize { get => _newItemFontSize; set { _newItemFontSize = Math.Round(value); OnPropertyChanged(); } }

        private bool _newItemIsBold = false;
        public bool NewItemIsBold { get => _newItemIsBold; set { _newItemIsBold = value; OnPropertyChanged(); } }

        private int _copies = 1;
        public int Copies { get => _copies; set { _copies = value; OnPropertyChanged(); } }

        private string _statusMessage;
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        private double _printAreaWidth;
        public double PrintAreaWidth
        {
            get => _printAreaWidth;
            set { _printAreaWidth = value; OnPropertyChanged(); OnPropertyChanged(nameof(PrintAreaSizeInfo)); }
        }

        private double _printAreaHeight;
        public double PrintAreaHeight
        {
            get => _printAreaHeight;
            set { _printAreaHeight = value; OnPropertyChanged(); OnPropertyChanged(nameof(PrintAreaSizeInfo)); }
        }

        private Thickness _printMargin;
        public Thickness PrintMargin { get => _printMargin; set { _printMargin = value; OnPropertyChanged(); } }

        public string PrintAreaSizeInfo => $"画布: {PrintAreaWidth:F0} x {PrintAreaHeight:F0} px";

        private PaperSize _selectedPaperSize;
        public PaperSize SelectedPaperSize
        {
            get => _selectedPaperSize;
            set { _selectedPaperSize = value; OnPropertyChanged(); UpdateLayout(); }
        }

        private MarginSetting _selectedMargin;
        public MarginSetting SelectedMargin
        {
            get => _selectedMargin;
            set { _selectedMargin = value; OnPropertyChanged(); UpdateLayout(); }
        }
        #endregion

        #region Methods

        private void ExecuteRemoveItem(object parameter)
        {
            if (parameter is PrintItemBase item)
            {
                PrintItems.Remove(item);
                StatusMessage = "元素已删除";
            }
        }

        private void UpdateLayout()
        {
            if (SelectedPaperSize == null || SelectedMargin == null) return;
            double scale = 96.0 / 25.4;
            double paperW = SelectedPaperSize.Width * scale;
            double paperH = SelectedPaperSize.Height * scale;
            double margin = SelectedMargin.Margin * scale;

            PrintMargin = new Thickness(margin);
            PrintAreaWidth = paperW - (margin * 2);
            PrintAreaHeight = paperH - (margin * 2);

            if (PrintAreaWidth < 0) PrintAreaWidth = 0;
            if (PrintAreaHeight < 0) PrintAreaHeight = 0;
        }

        private void ExecuteAddTextItem(object obj)
        {
            // 智能计算位置
            double yPos = 10 + (PrintItems.Count * 25);
            if (yPos > Math.Max(50, PrintAreaHeight - 50)) yPos = 10;

            var textSize = MeasureText(NewItemText, NewItemFontSize, NewItemIsBold);
            double w = Math.Max(80, textSize.Width + 10);
            double h = Math.Max(30, textSize.Height + 5);

            PrintItems.Add(new TextPrintItem
            {
                Content = NewItemText,
                X = 10,
                Y = yPos,
                FontSize = NewItemFontSize,
                IsBold = NewItemIsBold,
                Width = w,
                Height = h
            });
            StatusMessage = "已添加文本";
        }

        private void ExecuteAddQrCodeItem(object obj)
        {
            double yPos = 50;
            if (PrintItems.Any(p => Math.Abs(p.Y - yPos) < 10 && Math.Abs(p.X - 50) < 10)) yPos += 20;

            PrintItems.Add(new QrCodePrintItem
            {
                QrContent = NewQrCodeContent,
                X = 50,
                Y = yPos,
                Width = 80,
                Height = 80
            });
            StatusMessage = "已添加二维码";
        }

        private void ExecuteResetPosition(object obj)
        {
            double y = 10;
            foreach (var item in PrintItems)
            {
                item.X = 10;
                item.Y = y;
                y += item.Height + 5;
                if (y > PrintAreaHeight - 20) y = 10;
            }
        }

        private Canvas CreateCleanPrintCanvas()
        {
            double scale = 96.0 / 25.4;
            double paperW = SelectedPaperSize.Width * scale;
            double paperH = SelectedPaperSize.Height * scale;
            double margin = SelectedMargin.Margin * scale;

            Canvas cleanCv = new Canvas
            {
                Width = paperW,
                Height = paperH,
                Background = Brushes.White
            };

            foreach (var item in PrintItems)
            {
                FrameworkElement visualElement = null;
                double finalX = item.X + margin;
                double finalY = item.Y + margin;

                if (item is TextPrintItem t)
                {
                    var tb = new TextBlock
                    {
                        Text = t.Content,
                        FontSize = t.FontSize,
                        FontWeight = t.IsBold ? FontWeights.Bold : FontWeights.Normal,
                        Width = t.Width,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.Black,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    visualElement = tb;
                }
                else if (item is QrCodePrintItem q)
                {
                    var img = new Image
                    {
                        Source = q.QrImageSource,
                        Width = q.Width,
                        Height = q.Height,
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

            Size size = new Size(paperW, paperH);
            cleanCv.Measure(size);
            cleanCv.Arrange(new Rect(new Point(0, 0), size));
            cleanCv.UpdateLayout();

            return cleanCv;
        }

        private bool CanExecutePrint(object obj) => PrintItems.Count > 0;

        // 【关键修复】打印逻辑
        private void ExecutePrint(object obj)
        {
            if (SelectedPrinter == null)
            {
                MessageBox.Show("请先选择打印机", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                PrintDialog pd = new PrintDialog();
                pd.PrintQueue = SelectedPrinter;

                // 1. 【核心修复】设置 PrintTicket 纸张大小和份数
                if (SelectedPaperSize != null)
                {
                    double scale = 96.0 / 25.4;
                    double w = SelectedPaperSize.Width * scale;
                    double h = SelectedPaperSize.Height * scale;

                    // 设置页面尺寸，这对于标签打印机至关重要
                    pd.PrintTicket.PageMediaSize = new PageMediaSize(w, h);
                    pd.PrintTicket.CopyCount = Copies;
                }

                // 2. 生成纯净画布
                Canvas canvasToPrint = CreateCleanPrintCanvas();

                // 3. 发送打印
                pd.PrintVisual(canvasToPrint, "Label Print Job");

                StatusMessage = $"已发送 {Copies} 份至打印机";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印发生错误:\n{ex.Message}", "打印失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportConfig(object obj)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Print Config (*.json)|*.json";
                sfd.FileName = $"Config_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                if (sfd.ShowDialog() != true) return;

                var config = new PrintConfig
                {
                    PrinterName = SelectedPrinter?.Name,
                    PaperWidth = SelectedPaperSize?.Width ?? 0,
                    PaperHeight = SelectedPaperSize?.Height ?? 0,
                    Margin = SelectedMargin?.Margin ?? 0,
                    Copies = Copies
                };

                foreach (var item in PrintItems)
                {
                    var dto = new PrintItemDto
                    {
                        X = item.X,
                        Y = item.Y,
                        Width = item.Width,
                        Height = item.Height
                    };

                    if (item is TextPrintItem t)
                    {
                        dto.ItemType = "Text";
                        dto.Content = t.Content;
                        dto.FontSize = t.FontSize;
                        dto.IsBold = t.IsBold;
                    }
                    else if (item is QrCodePrintItem q)
                    {
                        dto.ItemType = "QrCode";
                        dto.Content = q.QrContent;
                    }
                    config.Items.Add(dto);
                }

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(sfd.FileName, json);

                StatusMessage = $"配置已导出: {Path.GetFileName(sfd.FileName)}";
                MessageBox.Show($"配置已保存至:\n{sfd.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出配置失败: {ex.Message}", "错误");
            }
        }

        private void ExecuteImportConfig(object obj)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Print Config (*.json)|*.json";
                if (ofd.ShowDialog() != true) return;

                string json = File.ReadAllText(ofd.FileName);
                PrintConfig config = JsonConvert.DeserializeObject<PrintConfig>(json);

                if (config == null) return;

                var paper = PaperSizes.FirstOrDefault(p => Math.Abs(p.Width - config.PaperWidth) < 0.1 && Math.Abs(p.Height - config.PaperHeight) < 0.1);
                if (paper == null)
                {
                    paper = new PaperSize($"自定义 ({config.PaperWidth}x{config.PaperHeight}mm)", config.PaperWidth, config.PaperHeight);
                    PaperSizes.Add(paper);
                }
                SelectedPaperSize = paper;

                var margin = Margins.FirstOrDefault(m => Math.Abs(m.Margin - config.Margin) < 0.1);
                if (margin == null)
                {
                    margin = new MarginSetting($"自定义 ({config.Margin}mm)", config.Margin);
                    Margins.Add(margin);
                }
                SelectedMargin = margin;

                Copies = config.Copies;

                if (!string.IsNullOrEmpty(config.PrinterName))
                {
                    var printer = AvailablePrinters.FirstOrDefault(p => p.Name == config.PrinterName);
                    if (printer != null) SelectedPrinter = printer;
                }

                PrintItems.Clear();
                foreach (var dto in config.Items)
                {
                    if (dto.ItemType == "Text")
                    {
                        PrintItems.Add(new TextPrintItem
                        {
                            Content = dto.Content,
                            X = dto.X,
                            Y = dto.Y,
                            Width = dto.Width,
                            Height = dto.Height,
                            FontSize = dto.FontSize,
                            IsBold = dto.IsBold
                        });
                    }
                    else if (dto.ItemType == "QrCode")
                    {
                        PrintItems.Add(new QrCodePrintItem
                        {
                            QrContent = dto.Content,
                            X = dto.X,
                            Y = dto.Y,
                            Width = dto.Width,
                            Height = dto.Height
                        });
                    }
                }
                StatusMessage = "配置导入完成";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入配置失败: {ex.Message}", "错误");
            }
        }

        private void ExecuteExportCpcl(object obj)
        {
            try
            {
                if (SelectedPaperSize == null || SelectedMargin == null) return;
                var processor = new CpclProcessor();
                string commands = processor.GenerateCpcl(PrintItems, SelectedPaperSize, SelectedMargin, Copies);
                string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                string fileName = $"Label_{DateTime.Now:yyyyMMdd_HHmmss}";
                string txtPath = Path.Combine(outputDir, $"{fileName}.txt");
                string pngPath = Path.Combine(outputDir, $"{fileName}.png");

                File.WriteAllText(txtPath, commands, Encoding.GetEncoding("GBK"));
                Canvas cleanCanvas = CreateCleanPrintCanvas();
                processor.SavePreviewImage(cleanCanvas, pngPath);

                StatusMessage = $"已导出: {fileName}";
                MessageBox.Show($"指令与预览图已保存至:\n{outputDir}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误");
            }
        }

        private void ExecuteImportCpcl(object obj)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog { Filter = "CPCL File|*.txt;*.cpcl" };
                if (dlg.ShowDialog() != true) return;

                var processor = new CpclProcessor();
                var items = processor.ParseCpcl(dlg.FileName, SelectedMargin);
                PrintItems.Clear();
                foreach (var item in items) PrintItems.Add(item);
                StatusMessage = "导入成功";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}");
            }
        }

        private Size MeasureText(string text, double fontSize, bool isBold)
        {
            if (string.IsNullOrEmpty(text)) return new Size(0, 0);
            var typeface = new Typeface(new FontFamily("Microsoft YaHei"), FontStyles.Normal,
                isBold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
            var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, fontSize, Brushes.Black, new NumberSubstitution(), 1.0);
            return new Size(ft.Width, ft.Height);
        }

        private void InitializePaperSizes()
        {
            PaperSizes.Add(new PaperSize("60×40mm (标签)", 60, 40));
            PaperSizes.Add(new PaperSize("50×30mm (标签)", 50, 30));
            PaperSizes.Add(new PaperSize("40×30mm (标签)", 40, 30));
        }

        private void InitializeMargins()
        {
            Margins.Add(new MarginSetting("无边距", 0));
            Margins.Add(new MarginSetting("极窄 (1mm)", 1));
            Margins.Add(new MarginSetting("标准 (2mm)", 2));
        }

        private void LoadPrinters()
        {
            try
            {
                var server = new LocalPrintServer();
                foreach (var q in server.GetPrintQueues()) AvailablePrinters.Add(q);
                SelectedPrinter = server.DefaultPrintQueue;
            }
            catch { }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // ==========================================
    //  模型类定义 (用于 JSON 序列化)
    // ==========================================
    public class PrintConfig
    {
        public string PrinterName { get; set; }
        public double PaperWidth { get; set; }
        public double PaperHeight { get; set; }
        public double Margin { get; set; }
        public int Copies { get; set; }
        public List<PrintItemDto> Items { get; set; } = new List<PrintItemDto>();
    }

    public class PrintItemDto
    {
        public string ItemType { get; set; } // "Text" or "QrCode"
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        // 文本属性
        public string Content { get; set; }
        public double FontSize { get; set; }
        public bool IsBold { get; set; }
    }

    public class CpclProcessor
    {
        private const double DotsPerMm = 8.0;
        private const double WpfDipPerMm = 96.0 / 25.4;
        private double ConversionFactor => DotsPerMm / WpfDipPerMm;

        public string GenerateCpcl(IEnumerable<PrintItemBase> items, PaperSize size, MarginSetting margin, int copies)
        {
            StringBuilder sb = new StringBuilder();
            int h = (int)(size.Height * DotsPerMm);
            int w = (int)(size.Width * DotsPerMm);
            int m = (int)(margin.Margin * DotsPerMm);

            sb.AppendLine($"! 0 200 200 {h} {copies}");
            sb.AppendLine($"PAGE-WIDTH {w}");

            foreach (var item in items)
            {
                int x = (int)Math.Round(item.X * ConversionFactor) + m;
                int y = (int)Math.Round(item.Y * ConversionFactor) + m;

                if (item is TextPrintItem t)
                {
                    int baseH = 24;
                    int targetH = (int)(t.FontSize * ConversionFactor);
                    int mag = targetH > baseH ? targetH / baseH : 1;
                    if (mag > 4) mag = 4;

                    if (t.IsBold) sb.AppendLine("SETBOLD 1");
                    if (mag > 1) sb.AppendLine($"SETMAG {mag} {mag}");

                    var lines = t.Content.Replace("\r\n", "\n").Split('\n');
                    int curY = y;
                    int lineH = (baseH * mag) + 6;

                    foreach (var line in lines)
                    {
                        sb.AppendLine($"TEXT 7 0 {x} {curY} {line}");
                        curY += lineH;
                    }

                    if (mag > 1) sb.AppendLine("SETMAG 0 0");
                    if (t.IsBold) sb.AppendLine("SETBOLD 0");
                }
                else if (item is QrCodePrintItem q)
                {
                    double targetDots = q.Width * ConversionFactor;
                    int u = (int)Math.Round(targetDots / 33.0);
                    if (u < 1) u = 1;
                    sb.AppendLine($"BARCODE QR {x} {y} M 2 U {u}");
                    sb.AppendLine($"MA,{q.QrContent}");
                    sb.AppendLine("ENDQR");
                }
            }
            sb.AppendLine("PRINT");
            return sb.ToString();
        }

        public List<PrintItemBase> ParseCpcl(string path, MarginSetting margin)
        {
            var list = new List<PrintItemBase>();
            string[] lines = File.ReadAllLines(path, Encoding.GetEncoding("UTF-8"));
            int m = (int)(margin.Margin * DotsPerMm);
            int magH = 1;
            bool bold = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                var parts = line.Split(' ');

                if (line.StartsWith("SETMAG") && parts.Length > 2)
                    int.TryParse(parts[2], out magH);
                else if (line.StartsWith("SETBOLD"))
                    bold = parts.Length > 1 && parts[1] == "1";
                else if (line.StartsWith("TEXT") && parts.Length >= 5)
                {
                    if (int.TryParse(parts[3], out int x) && int.TryParse(parts[4], out int y))
                    {
                        string content = GetTextContent(line);
                        double wx = (x - m) / ConversionFactor;
                        double wy = (y - m) / ConversionFactor;
                        double fs = (24 * magH) / ConversionFactor;

                        var size = MeasureText(content, fs, bold);
                        list.Add(new TextPrintItem
                        {
                            Content = content,
                            X = wx,
                            Y = wy,
                            FontSize = fs,
                            IsBold = bold,
                            Width = size.Width + 10,
                            Height = size.Height + 5
                        });
                    }
                }
                else if (line.StartsWith("BARCODE QR") && parts.Length >= 4)
                {
                    if (int.TryParse(parts[2], out int x) && int.TryParse(parts[3], out int y))
                    {
                        double u = 4;
                        for (int k = 0; k < parts.Length; k++) if (parts[k] == "U" && k + 1 < parts.Length) double.TryParse(parts[k + 1], out u);
                        if (i + 1 < lines.Length)
                        {
                            string data = lines[++i].Replace("MA,", "");
                            double wx = (x - m) / ConversionFactor;
                            double wy = (y - m) / ConversionFactor;
                            double sz = (u * 33) / ConversionFactor;
                            list.Add(new QrCodePrintItem { QrContent = data, X = wx, Y = wy, Width = sz, Height = sz });
                        }
                    }
                }
            }
            return list;
        }

        private string GetTextContent(string line)
        {
            int spaces = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ') spaces++;
                if (spaces == 5) return line.Substring(i + 1);
            }
            return "";
        }

        private Size MeasureText(string text, double fontSize, bool isBold)
        {
            if (string.IsNullOrEmpty(text)) return new Size(0, 0);
            var typeface = new Typeface(new FontFamily("Microsoft YaHei"), FontStyles.Normal,
                isBold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
            var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, fontSize, Brushes.Black, new NumberSubstitution(), 1.0);
            return new Size(ft.Width, ft.Height);
        }

        public void SavePreviewImage(Canvas cvs, string path)
        {
            int w = (int)cvs.Width;
            int h = (int)cvs.Height;
            if (w <= 0 || h <= 0) return;
            RenderTargetBitmap rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(cvs);
            PngBitmapEncoder enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));
            using (Stream s = File.Create(path)) enc.Save(s);
        }
    }
}