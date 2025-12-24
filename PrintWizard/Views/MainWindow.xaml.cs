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
    /// <summary>
    /// MainWindow 的交互逻辑
    /// 负责处理与界面强相关的鼠标拖拽、缩放等交互操作
    /// </summary>
    public partial class MainWindow : Window
    {
        // 拖拽状态变量
        private bool _isDragging = false;
        private Point _clickPosition;
        private double _originalLeft, _originalTop;
        private FrameworkElement _selectedElement;

        // 获取当前的 ViewModel 实例
        private PrintViewModel ViewModel => DataContext as PrintViewModel;

        public MainWindow()
        {
            InitializeComponent();

            // [修正] ViewModel 已重构，不再需要在构造函数中传入 Canvas
            if (DataContext == null)
            {
                DataContext = new PrintViewModel();
            }
        }

        /// <summary>
        /// 辅助方法：查找可视树中指定类型的父控件
        /// 用于区分点击的是控件本身，还是其内部的按钮或手柄
        /// </summary>
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

        #region 拖拽逻辑 (Drag & Drop)

        /// <summary>
        /// 鼠标按下：开始拖拽
        /// </summary>
        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 1. 防拦截检查：如果点击的是调整手柄(Thumb)或按钮(Button)，则不触发拖拽
            if (FindVisualParent<Button>(e.OriginalSource as DependencyObject) != null) return;
            if (FindVisualParent<Thumb>(e.OriginalSource as DependencyObject) != null) return;

            // 2. 获取被点击的元素
            _selectedElement = sender as FrameworkElement;
            if (_selectedElement == null) return;

            // 3. 记录初始状态
            _isDragging = true;
            _clickPosition = e.GetPosition(ContentCanvas);

            var item = _selectedElement.DataContext as PrintItemBase;
            if (item != null)
            {
                _originalLeft = item.X;
                _originalTop = item.Y;
            }

            // 4. 捕获鼠标，确保拖拽流畅
            _selectedElement.CaptureMouse();
            e.Handled = true;
        }

        /// <summary>
        /// 鼠标移动：更新位置
        /// </summary>
        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _selectedElement == null) return;

            // 计算位移量
            var currentPos = e.GetPosition(ContentCanvas);
            double deltaX = currentPos.X - _clickPosition.X;
            double deltaY = currentPos.Y - _clickPosition.Y;

            var item = _selectedElement.DataContext as PrintItemBase;
            if (item != null)
            {
                double newX = _originalLeft + deltaX;
                double newY = _originalTop + deltaY;

                // 左上边界限制
                if (newX < 0) newX = 0;
                if (newY < 0) newY = 0;

                // 右下边界限制 (防止拖出画布)
                if (ViewModel != null)
                {
                    if (newX + item.Width > ViewModel.PrintAreaWidth)
                        newX = Math.Max(0, ViewModel.PrintAreaWidth - item.Width);

                    if (newY + item.Height > ViewModel.PrintAreaHeight)
                        newY = Math.Max(0, ViewModel.PrintAreaHeight - item.Height);
                }

                // 更新数据模型，UI 会自动更新 (双向绑定)
                item.X = newX;
                item.Y = newY;
            }
        }

        /// <summary>
        /// 鼠标松开：结束拖拽
        /// </summary>
        private void Item_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _selectedElement?.ReleaseMouseCapture();
                _selectedElement = null;
            }
        }

        #endregion

        #region 缩放逻辑 (Resizing)

        /// <summary>
        /// 处理右下角手柄的拖拽事件，调整控件大小
        /// </summary>
        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            var item = thumb?.DataContext as PrintItemBase;

            if (item != null)
            {
                // 计算新尺寸
                double newW = item.Width + e.HorizontalChange;
                double newH = item.Height + e.VerticalChange;

                // 最小尺寸限制
                newW = Math.Max(newW, 20);
                newH = Math.Max(newH, 20);

                // 特殊处理：二维码保持正方形
                if (item is QrCodePrintItem)
                {
                    double size = Math.Max(newW, newH);
                    newW = size;
                    newH = size;
                }

                // 边界限制：防止放大超出画布
                if (ViewModel != null)
                {
                    if (item.X + newW > ViewModel.PrintAreaWidth)
                        newW = ViewModel.PrintAreaWidth - item.X;

                    if (item.Y + newH > ViewModel.PrintAreaHeight)
                        newH = ViewModel.PrintAreaHeight - item.Y;

                    // 再次检查二维码比例 (因为边界限制可能只限制了宽或高)
                    if (item is QrCodePrintItem)
                    {
                        double size = Math.Min(newW, newH);
                        newW = size;
                        newH = size;
                    }
                }

                item.Width = newW;
                item.Height = newH;

                // 标记事件已处理，防止冒泡触发 Drag
                e.Handled = true;
            }
        }

        #endregion
    }
}