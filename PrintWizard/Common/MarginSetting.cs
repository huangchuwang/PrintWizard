using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintWizard.Common
{
    /// <summary>
    /// 边距模型
    /// </summary>
    public class MarginSetting
    {
        public string DisplayName { get; set; }
        public double Margin { get; set; }
        public MarginSetting(string displayName, double margin) { DisplayName = displayName; Margin = margin; }
    }

}
