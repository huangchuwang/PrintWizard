using PrintWizard.Models;
using PrintWizard.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace PrintWizard.Views
{
    public partial class MainWindow : Window
    {
        private bool _isDragging = false;
        private Point _clickPosition;
        private double _originalLeft, _originalTop;
        private FrameworkElement _selectedElement;

        private PrintViewModel ViewModel => DataContext as PrintViewModel;

        public MainWindow()
        {
            InitializeComponent();
            if (DataContext == null)
            {
                DataContext = new PrintViewModel(ContentCanvas);
            }
        }

        // 辅助方法：查找指定类型的父控件
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = child;
            while (parentObject != null)
            {
                if (parentObject is T parent) return parent;
                parentObject = VisualTreeHelper.GetParent(parentObject);
            }
            return null;
        }

        // === 拖拽逻辑 ===
        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 【修复核心】：如果点击的是按钮（无论是否叫 DeleteBtn）或调整手柄，
            // 则直接返回，不执行拖拽逻辑，让控件自己的事件（如 Command）继续触发。
            if (FindVisualParent<Button>(e.OriginalSource as DependencyObject) != null) return;
            if (FindVisualParent<Thumb>(e.OriginalSource as DependencyObject) != null) return;

            _selectedElement = sender as FrameworkElement;
            if (_selectedElement == null) return;

            _isDragging = true;
            _clickPosition = e.GetPosition(ContentCanvas);

            var item = _selectedElement.DataContext as PrintItemBase;
            if (item != null)
            {
                _originalLeft = item.X;
                _originalTop = item.Y;
            }

            _selectedElement.CaptureMouse();
            e.Handled = true;
        }

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _selectedElement == null) return;

            var currentPos = e.GetPosition(ContentCanvas);
            double deltaX = currentPos.X - _clickPosition.X;
            double deltaY = currentPos.Y - _clickPosition.Y;

            var item = _selectedElement.DataContext as PrintItemBase;
            if (item != null)
            {
                double newX = _originalLeft + deltaX;
                double newY = _originalTop + deltaY;

                if (newX < 0) newX = 0;
                if (newY < 0) newY = 0;

                if (ViewModel != null)
                {
                    if (newX + item.Width > ViewModel.PrintAreaWidth)
                        newX = Math.Max(0, ViewModel.PrintAreaWidth - item.Width);

                    if (newY + item.Height > ViewModel.PrintAreaHeight)
                        newY = Math.Max(0, ViewModel.PrintAreaHeight - item.Height);
                }

                item.X = newX;
                item.Y = newY;
            }
        }

        private void Item_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _selectedElement?.ReleaseMouseCapture();
                _selectedElement = null;
            }
        }

        // === 缩放逻辑 ===
        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            var item = thumb?.DataContext as PrintItemBase;

            if (item != null)
            {
                double newW = item.Width + e.HorizontalChange;
                double newH = item.Height + e.VerticalChange;

                newW = Math.Max(newW, 20);
                newH = Math.Max(newH, 20);

                // 二维码保持正方形
                if (item is QrCodePrintItem)
                {
                    double size = Math.Max(newW, newH);
                    newW = size;
                    newH = size;
                }

                if (ViewModel != null)
                {
                    if (item.X + newW > ViewModel.PrintAreaWidth)
                        newW = ViewModel.PrintAreaWidth - item.X;

                    if (item.Y + newH > ViewModel.PrintAreaHeight)
                        newH = ViewModel.PrintAreaHeight - item.Y;

                    if (item is QrCodePrintItem)
                    {
                        double size = Math.Min(newW, newH);
                        newW = size;
                        newH = size;
                    }
                }

                item.Width = newW;
                item.Height = newH;
                e.Handled = true;
            }
        }
        // 旧的 DeleteItem_Click 已被 Command 取代，无需保留
    }
}