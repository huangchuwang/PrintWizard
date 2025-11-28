using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using PrintWizard.Models;
using PrintWizard.ViewModels;

namespace PrintWizard.Views
{
    public partial class MainWindow : Window
    {
        private bool isDragging = false;
        private Point startPoint; // 原始代码中这里存储的是 Canvas 上的起始点
        private FrameworkElement draggedElement;
        private PrintItemBase draggedItem;
        private PrintViewModel? vm => DataContext as PrintViewModel;

        public MainWindow()
        {
            InitializeComponent();
            // 注意：在 MainWindow.xaml 中，ContentCanvas 必须在 InitializeComponent 后才存在
            var viewModel = new PrintViewModel(ContentCanvas);
            DataContext = viewModel;
        }

        // 鼠标按下：准备拖拽 (原始代码中的逻辑)
        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 【重要逻辑】检查点击源
            // 如果点击的是删除按钮或调整大小手柄，则不启动整体拖拽
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

            isDragging = true;

            // 原始代码中，startPoint 存储的是鼠标在 Canvas 上的绝对位置
            // 这是导致拖拽精度问题的关键，因为拖拽时只应计算偏移量
            startPoint = e.GetPosition(ContentCanvas);

            draggedElement.CaptureMouse();

            if (draggedElement != null)
            {
                Panel.SetZIndex(draggedElement, 999);
            }

            e.Handled = true;
        }

        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        // 鼠标移动：拖拽逻辑
        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == MouseButtonState.Pressed && vm != null && draggedItem != null)
            {
                //当前鼠标位置
                var currentPoint = e.GetPosition(ContentCanvas);

                // 使用当前鼠标位置减去起始鼠标位置，得到位移量
                // 它计算的是 (当前鼠标 - 起始鼠标)，然后将位移量加到元素的 X/Y 上。
                double deltaX = currentPoint.X - startPoint.X;
                double deltaY = currentPoint.Y - startPoint.Y;

                double newX = draggedItem.X + deltaX;
                double newY = draggedItem.Y + deltaY;

                // 边界检查
                double printLeft = 0;
                double printTop = 0;
                double itemWidth = draggedItem.Width;
                double itemHeight = draggedItem.Height;

                double maxNewX = vm.PrintAreaWidth - itemWidth;
                double maxNewY = vm.PrintAreaHeight - itemHeight;

                newX = System.Math.Max(printLeft, System.Math.Min(newX, maxNewX));
                newY = System.Math.Max(printTop, System.Math.Min(newY, maxNewY));

                if (maxNewX < printLeft) newX = printLeft;
                if (maxNewY < printTop) newY = printTop;


                if (Math.Abs(draggedItem.X - newX) > 0.1 || Math.Abs(draggedItem.Y - newY) > 0.1)
                {
                    draggedItem.X = newX;
                    draggedItem.Y = newY;
                }

                // 更新 startPoint 以供下一次移动计算
                startPoint = currentPoint;
            }
        }

        private void Item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            if (draggedElement != null)
            {
                draggedElement.ReleaseMouseCapture();
                Panel.SetZIndex(draggedElement, 1);
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

                newWidth = Math.Max(newWidth, 30);
                newHeight = Math.Max(newHeight, 30);

                // 边界限制：不能超出 Canvas 区域
                if (item.X + newWidth > vm.PrintAreaWidth) newWidth = vm.PrintAreaWidth - item.X;
                if (item.Y + newHeight > vm.PrintAreaHeight) newHeight = vm.PrintAreaHeight - item.Y;

                item.Width = newWidth;
                item.Height = newHeight;
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