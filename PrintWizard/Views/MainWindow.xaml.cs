using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PrintWizard.Models;
using PrintWizard.ViewModels;

namespace PrintWizard.Views
{
    public partial class MainWindow : Window
    {
        // 拖拽相关状态
        private bool isDragging = false;
        private Point startClickPoint;
        private double originalItemX;
        private double originalItemY;

        // 长按检测
        private DispatcherTimer longPressTimer;
        private const int LongPressThresholdMs = 200;
        private const double DragCancelThreshold = 3.0;

        private FrameworkElement draggedElement;
        private PrintItemBase draggedItem;
        private PrintViewModel? vm => DataContext as PrintViewModel;

        public MainWindow()
        {
            InitializeComponent();

            longPressTimer = new DispatcherTimer();
            longPressTimer.Interval = TimeSpan.FromMilliseconds(LongPressThresholdMs);
            longPressTimer.Tick += LongPressTimer_Tick;

            var viewModel = new PrintViewModel(ContentCanvas);
            DataContext = viewModel;
        }

        private void LongPressTimer_Tick(object? sender, EventArgs e)
        {
            longPressTimer.Stop();

            if (draggedElement != null && draggedItem != null)
            {
                isDragging = true;
                draggedElement.CaptureMouse();
                Panel.SetZIndex(draggedElement, 999);
                draggedElement.Cursor = Cursors.SizeAll;
            }
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject originalSource)
            {
                var parentBtn = FindVisualParent<Button>(originalSource);
                var parentThumb = FindVisualParent<Thumb>(originalSource);

                if (parentBtn != null && parentBtn.Name == "DeleteBtn") return;
                if (parentThumb != null) return;
            }

            draggedElement = sender as FrameworkElement;
            draggedItem = draggedElement?.DataContext as PrintItemBase;

            if (draggedElement == null || draggedItem == null) return;

            isDragging = false;
            startClickPoint = e.GetPosition(ContentCanvas);
            originalItemX = draggedItem.X;
            originalItemY = draggedItem.Y;

            longPressTimer.Start();
        }

        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPoint = e.GetPosition(ContentCanvas);

            if (longPressTimer.IsEnabled)
            {
                if (Math.Abs(currentPoint.X - startClickPoint.X) > DragCancelThreshold ||
                    Math.Abs(currentPoint.Y - startClickPoint.Y) > DragCancelThreshold)
                {
                    longPressTimer.Stop();
                    return;
                }
            }

            if (isDragging && vm != null && draggedItem != null)
            {
                double deltaX = currentPoint.X - startClickPoint.X;
                double deltaY = currentPoint.Y - startClickPoint.Y;

                double newX = originalItemX + deltaX;
                double newY = originalItemY + deltaY;

                double printLeft = 0;
                double printTop = 0;
                double itemWidth = draggedItem.Width;
                double itemHeight = draggedItem.Height;

                double maxNewX = vm.PrintAreaWidth - itemWidth;
                double maxNewY = vm.PrintAreaHeight - itemHeight;

                newX = Math.Max(printLeft, Math.Min(newX, maxNewX));
                newY = Math.Max(printTop, Math.Min(newY, maxNewY));

                if (maxNewX < printLeft) newX = printLeft;
                if (maxNewY < printTop) newY = printTop;

                draggedItem.X = newX;
                draggedItem.Y = newY;

                e.Handled = true;
            }
        }

        private void Item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (longPressTimer.IsEnabled)
            {
                longPressTimer.Stop();
            }

            if (isDragging)
            {
                isDragging = false;
                if (draggedElement != null)
                {
                    draggedElement.ReleaseMouseCapture();
                    Panel.SetZIndex(draggedElement, 1);
                    draggedElement.Cursor = null;
                }
                e.Handled = true;
            }

            draggedElement = null;
            draggedItem = null;
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            var item = thumb?.DataContext as PrintItemBase;

            if (item != null && vm != null)
            {
                double newWidth = item.Width + e.HorizontalChange;
                double newHeight = item.Height + e.VerticalChange;

                newWidth = Math.Max(newWidth, 20);
                newHeight = Math.Max(newHeight, 20);

                // 【核心修改】如果是二维码，强制保持正方形比例
                if (item is QrCodePrintItem)
                {
                    // 取宽高中的最大值作为新的边长，保证不被压扁且操作符合直觉
                    double size = Math.Max(newWidth, newHeight);
                    newWidth = size;
                    newHeight = size;
                }

                // 边界限制
                if (item.X + newWidth > vm.PrintAreaWidth) newWidth = vm.PrintAreaWidth - item.X;
                if (item.Y + newHeight > vm.PrintAreaHeight) newHeight = vm.PrintAreaHeight - item.Y;

                // 二维码再次检查边界导致的比例失调（如果宽度受限，高度也要跟着受限）
                if (item is QrCodePrintItem)
                {
                    double size = Math.Min(newWidth, newHeight);
                    newWidth = size;
                    newHeight = size;
                }

                item.Width = newWidth;
                item.Height = newHeight;

                e.Handled = true;
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var itemToDelete = (sender as FrameworkElement)?.DataContext as PrintItemBase;
            if (itemToDelete != null && vm != null)
            {
                vm.PrintItems.Remove(itemToDelete);
            }
        }
    }
}