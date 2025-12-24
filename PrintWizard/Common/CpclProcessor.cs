using PrintWizard.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrintWizard.Common
{
    /// <summary>
    /// 处理CPCL指令
    /// </summary>
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
