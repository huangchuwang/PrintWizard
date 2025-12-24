using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace PrintWizard.Common
{
    /// <summary>
    /// 所有的静态计算逻辑，如单位换算和文本测量
    /// </summary>
    public static class PrintUtils
    {
        // 96 DPI 下，1英寸 = 25.4mm
        public const double MmToDipFactor = 96.0 / 25.4;

        // CPCL 203 DPI 下，1mm = 8 dots
        public const double DotsPerMm = 8.0;

        public static double MmToDip(double mm) => mm * MmToDipFactor;
        public static double DipToMm(double dip) => dip / MmToDipFactor;

        // 用于 ViewModel 更新布局
        public static (Thickness MarginThickness, double PrintableWidth, double PrintableHeight) CalculateLayout(double paperW_mm, double paperH_mm, double margin_mm)
        {
            double scale = MmToDipFactor;
            double paperW = paperW_mm * scale;
            double paperH = paperH_mm * scale;
            double margin = margin_mm * scale;

            double pW = Math.Max(0, paperW - (margin * 2));
            double pH = Math.Max(0, paperH - (margin * 2));

            return (new Thickness(margin), pW, pH);
        }

        public static Size MeasureText(string text, double fontSize, bool isBold)
        {
            if (string.IsNullOrEmpty(text)) return new Size(0, 0);

            var typeface = new Typeface(
                new FontFamily("Microsoft YaHei"),
                FontStyles.Normal,
                isBold ? FontWeights.Bold : FontWeights.Normal,
                FontStretches.Normal);

            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1.0); // 1.0 pixelsPerDip

            return new Size(ft.Width, ft.Height);
        }
    }
}
