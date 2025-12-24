using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintWizard.Common
{
    /// <summary>
    /// 打印纸张尺寸模型
    /// </summary>
    public class PaperSize
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public PaperSize(string name, double width, double height) { Name = name; Width = width; Height = height; }
    }
}
