using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintWizard.Models
{
    public class PrintConfig
    {
        public string PrinterName { get; set; }
        public double PaperWidth { get; set; }
        public double PaperHeight { get; set; }
        public double Margin { get; set; }
        public int Copies { get; set; }
        public List<PrintItemDto> Items { get; set; } = new List<PrintItemDto>();
    }
}
