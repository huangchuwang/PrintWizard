using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintWizard.Models
{
    public class PrintItemDto
    {
        public string ItemType { get; set; } // "Text" 或 "QrCode"
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        // 通用内容字段
        public string Content { get; set; }

        // 文本特有属性
        public double FontSize { get; set; }
        public bool IsBold { get; set; }
    }
}
