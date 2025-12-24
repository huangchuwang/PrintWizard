using Newtonsoft.Json;
using PrintWizard.Common;
using PrintWizard.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;

namespace PrintWizard.Service
{
    public class ConfigService
    {
        public PrintConfig CreateConfigFromViewModel(IEnumerable<PrintItemBase> items, PaperSize paper, MarginSetting margin, PrintQueue printer, int copies)
        {
            var config = new PrintConfig
            {
                PrinterName = printer?.Name,
                PaperWidth = paper?.Width ?? 0,
                PaperHeight = paper?.Height ?? 0,
                Margin = margin?.Margin ?? 0,
                Copies = copies
            };

            foreach (var item in items)
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
            return config;
        }

        public void SaveConfigToFile(PrintConfig config, string path)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public PrintConfig LoadConfigFromFile(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<PrintConfig>(json);
        }

        public IEnumerable<PrintItemBase> MapDtosToItems(List<PrintItemDto> dtos)
        {
            var list = new List<PrintItemBase>();
            foreach (var dto in dtos)
            {
                if (dto.ItemType == "Text")
                {
                    list.Add(new TextPrintItem
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
                    list.Add(new QrCodePrintItem
                    {
                        QrContent = dto.Content,
                        X = dto.X,
                        Y = dto.Y,
                        Width = dto.Width,
                        Height = dto.Height
                    });
                }
            }
            return list;
        }
    }
}
