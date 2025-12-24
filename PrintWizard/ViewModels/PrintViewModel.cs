using Microsoft.Win32;
using PrintWizard.Common;
using PrintWizard.Models;
using PrintWizard.Service;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrintWizard.ViewModels
{
    public class PrintViewModel : INotifyPropertyChanged
    {
        // 引用服务类
        private readonly CpclService _cpclService;
        private readonly CanvasService _canvasService;
        private readonly ConfigService _configService;

        public PrintViewModel()
        {
            // 注册编码支持 (建议放在 App.xaml.cs 启动时，但放在这里也没问题)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // 初始化服务
            _cpclService = new CpclService();
            _canvasService = new CanvasService();
            _configService = new ConfigService();

            // 初始化命令
            InitCommands();

            // 初始化数据
            InitializePaperSizes();
            InitializeMargins();
            LoadPrinters();

            // 默认设置
            SelectedPaperSize = PaperSizes.FirstOrDefault(p => p.Name.Contains("60×40mm")) ?? PaperSizes.FirstOrDefault();
            SelectedMargin = Margins.FirstOrDefault(m => m.DisplayName.Contains("极窄边距")) ?? Margins.FirstOrDefault();

            StatusMessage = "系统就绪";
        }

        #region Commands & Initialization
        public ICommand ExportCpclCommand { get; private set; }
        public ICommand ImportCpclCommand { get; private set; }
        public ICommand PrintCommand { get; private set; }
        public ICommand ResetPositionCommand { get; private set; }
        public ICommand AddTextItemCommand { get; private set; }
        public ICommand AddQrCodeItemCommand { get; private set; }
        public ICommand RemoveItemCommand { get; private set; }
        public ICommand ExportConfigCommand { get; private set; }
        public ICommand ImportConfigCommand { get; private set; }

        private void InitCommands()
        {
            PrintCommand = new RelayCommand(ExecutePrint, CanExecutePrint);
            ResetPositionCommand = new RelayCommand(ExecuteResetPosition);
            AddTextItemCommand = new RelayCommand(ExecuteAddTextItem);
            AddQrCodeItemCommand = new RelayCommand(ExecuteAddQrCodeItem);
            ExportCpclCommand = new RelayCommand(ExecuteExportCpcl, CanExecutePrint);
            ImportCpclCommand = new RelayCommand(ExecuteImportCpcl);
            RemoveItemCommand = new RelayCommand(ExecuteRemoveItem);
            ExportConfigCommand = new RelayCommand(ExecuteExportConfig, CanExecutePrint);
            ImportConfigCommand = new RelayCommand(ExecuteImportConfig);
        }
        #endregion

        #region Properties
        public ObservableCollection<PrintItemBase> PrintItems { get; } = new ObservableCollection<PrintItemBase>();
        public ObservableCollection<PaperSize> PaperSizes { get; } = new();
        public ObservableCollection<MarginSetting> Margins { get; } = new();
        public ObservableCollection<PrintQueue> AvailablePrinters { get; } = new();

        private PrintQueue _selectedPrinter;
        public PrintQueue SelectedPrinter
        {
            get => _selectedPrinter;
            set { _selectedPrinter = value; OnPropertyChanged(); }
        }

        private string _newItemText = "请输入文本";
        public string NewItemText { get => _newItemText; set { _newItemText = value; OnPropertyChanged(); } }

        private string _newQrCodeContent = "88888888";
        public string NewQrCodeContent { get => _newQrCodeContent; set { _newQrCodeContent = value; OnPropertyChanged(); } }

        private double _newItemFontSize = 12;
        public double NewItemFontSize { get => _newItemFontSize; set { _newItemFontSize = Math.Round(value); OnPropertyChanged(); } }

        private bool _newItemIsBold = false;
        public bool NewItemIsBold { get => _newItemIsBold; set { _newItemIsBold = value; OnPropertyChanged(); } }

        private int _copies = 1;
        public int Copies { get => _copies; set { _copies = value; OnPropertyChanged(); } }

        private string _statusMessage;
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        // 布局相关属性
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

        #region Core Logic Methods

        private void UpdateLayout()
        {
            if (SelectedPaperSize == null || SelectedMargin == null) return;

            // 使用工具类进行计算
            var layout = PrintUtils.CalculateLayout(SelectedPaperSize.Width, SelectedPaperSize.Height, SelectedMargin.Margin);

            PrintMargin = layout.MarginThickness;
            PrintAreaWidth = layout.PrintableWidth;
            PrintAreaHeight = layout.PrintableHeight;
        }

        private void ExecuteAddTextItem(object obj)
        {
            // 智能计算Y坐标
            double yPos = 10 + (PrintItems.Count * 25);
            if (yPos > Math.Max(50, PrintAreaHeight - 50)) yPos = 10;

            // 使用工具类测量文本
            var textSize = PrintUtils.MeasureText(NewItemText, NewItemFontSize, NewItemIsBold);

            PrintItems.Add(new TextPrintItem
            {
                Content = NewItemText,
                X = 10,
                Y = yPos,
                FontSize = NewItemFontSize,
                IsBold = NewItemIsBold,
                Width = Math.Max(80, textSize.Width + 10),
                Height = Math.Max(30, textSize.Height + 5)
            });
            StatusMessage = "已添加文本";
        }

        private void ExecuteAddQrCodeItem(object obj)
        {
            double yPos = 50;
            // 简单的防重叠策略
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

        private void ExecuteRemoveItem(object parameter)
        {
            if (parameter is PrintItemBase item)
            {
                PrintItems.Remove(item);
                StatusMessage = "元素已删除";
            }
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
            StatusMessage = "位置已重置";
        }

        private bool CanExecutePrint(object obj) => PrintItems.Count > 0;

        // === 打印逻辑 (委托给 CanvasService) ===
        private void ExecutePrint(object obj)
        {
            if (SelectedPrinter == null)
            {
                MessageBox.Show("请先选择打印机", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. 生成纯净画布
                Canvas canvasToPrint = _canvasService.CreateCleanPrintCanvas(PrintItems, SelectedPaperSize, SelectedMargin);

                // 2. 配置打印参数
                PrintDialog pd = new PrintDialog { PrintQueue = SelectedPrinter };

                if (SelectedPaperSize != null)
                {
                    // 转换单位 mm -> px (DIP)
                    double w = PrintUtils.MmToDip(SelectedPaperSize.Width);
                    double h = PrintUtils.MmToDip(SelectedPaperSize.Height);

                    pd.PrintTicket.PageMediaSize = new PageMediaSize(w, h);
                    pd.PrintTicket.CopyCount = Copies;
                }

                // 3. 打印
                pd.PrintVisual(canvasToPrint, "PrintWizard Print Job");
                StatusMessage = $"已发送 {Copies} 份至打印机";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === CPCL 导出 (委托给 CpclService 和 CanvasService) ===
        private void ExecuteExportCpcl(object obj)
        {
            try
            {
                if (SelectedPaperSize == null || SelectedMargin == null) return;

                // 生成指令
                string commands = _cpclService.GenerateCpcl(PrintItems, SelectedPaperSize, SelectedMargin, Copies);

                // 准备路径
                string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                string fileName = $"Label_{DateTime.Now:yyyyMMdd_HHmmss}";
                string txtPath = Path.Combine(outputDir, $"{fileName}.txt");
                string pngPath = Path.Combine(outputDir, $"{fileName}.png");

                // 保存文件 (GBK编码)
                File.WriteAllText(txtPath, commands, Encoding.GetEncoding("GBK"));

                // 保存预览图
                Canvas cleanCanvas = _canvasService.CreateCleanPrintCanvas(PrintItems, SelectedPaperSize, SelectedMargin);
                _canvasService.SaveCanvasToImage(cleanCanvas, pngPath);

                StatusMessage = $"已导出: {fileName}";
                MessageBox.Show($"指令与预览图已保存至:\n{outputDir}", "导出成功");
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

                // 解析指令
                var items = _cpclService.ParseCpcl(dlg.FileName, SelectedMargin);

                PrintItems.Clear();
                foreach (var item in items) PrintItems.Add(item);

                StatusMessage = "CPCL 导入成功";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}");
            }
        }

        // === JSON 配置导出/导入 (委托给 ConfigService) ===
        private void ExecuteExportConfig(object obj)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "Print Config (*.json)|*.json",
                    FileName = $"Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };
                if (sfd.ShowDialog() != true) return;

                var config = _configService.CreateConfigFromViewModel(PrintItems, SelectedPaperSize, SelectedMargin, SelectedPrinter, Copies);
                _configService.SaveConfigToFile(config, sfd.FileName);

                StatusMessage = "配置已导出";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出配置失败: {ex.Message}");
            }
        }

        private void ExecuteImportConfig(object obj)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "Print Config (*.json)|*.json" };
                if (ofd.ShowDialog() != true) return;

                var config = _configService.LoadConfigFromFile(ofd.FileName);
                if (config == null) return;

                RestoreFromConfig(config);
                StatusMessage = "配置导入完成";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入配置失败: {ex.Message}");
            }
        }

        private void RestoreFromConfig(PrintConfig config)
        {
            // 恢复页面设置 (尝试查找或创建自定义)
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

            // 恢复元素
            PrintItems.Clear();
            foreach (var item in _configService.MapDtosToItems(config.Items))
            {
                PrintItems.Add(item);
            }
        }

        #endregion

        #region Init Helpers
        private void InitializePaperSizes()
        {
            PaperSizes.Add(new PaperSize("60×40mm (标签)", 60, 40));
            PaperSizes.Add(new PaperSize("50×30mm (标签)", 50, 30));
            PaperSizes.Add(new PaperSize("40×30mm (标签)", 40, 30));
            PaperSizes.Add(new PaperSize("100×180mm (电子面单)", 100, 180));
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
            }catch{}
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}