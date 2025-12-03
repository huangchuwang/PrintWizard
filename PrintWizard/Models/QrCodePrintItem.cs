using QRCoder;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrintWizard.Models
{
    public class QrCodePrintItem : PrintItemBase
    {
        public override string ItemType => "QrCode";

        public QrCodePrintItem()
        {
            Width = 100;
            Height = 100;
        }

        private string qrContent;
        public string QrContent
        {
            get => qrContent;
            set
            {
                qrContent = value;
                OnPropertyChanged();
                QrImageSource = GenerateQrCodeBitmap(value);
            }
        }

        private ImageSource qrImageSource;
        public ImageSource QrImageSource
        {
            get => qrImageSource;
            set { qrImageSource = value; OnPropertyChanged(); }
        }

        private BitmapSource GenerateQrCodeBitmap(string content)
        {
            if (string.IsNullOrEmpty(content)) return null;
            try
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData))
                {
                    // 【关键修改】将最后一个参数改为 false
                    // GetGraphic(pixelsPerModule, darkColor, lightColor, drawQuietZones)
                    // drawQuietZones: false 表示不绘制默认的白色静区边框
                    using (var qrBitmap = qrCode.GetGraphic(20, System.Drawing.Color.Black, System.Drawing.Color.White, false))
                    {
                        using (MemoryStream memory = new MemoryStream())
                        {
                            qrBitmap.Save(memory, ImageFormat.Png);
                            memory.Position = 0;
                            BitmapImage bitmapimage = new BitmapImage();
                            bitmapimage.BeginInit();
                            bitmapimage.StreamSource = memory;
                            bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapimage.EndInit();
                            bitmapimage.Freeze();
                            return bitmapimage;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}