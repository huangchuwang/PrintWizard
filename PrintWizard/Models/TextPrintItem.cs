namespace PrintWizard.Models
{
    public class TextPrintItem : PrintItemBase
    {
        public override string ItemType => "Text";

        public TextPrintItem()
        {
            Width = 200;
            Height = 80;
        }

        private string content = "新的文本内容";
        public string Content
        {
            get => content;
            set { content = value; OnPropertyChanged(); }
        }

        // 字体大小属性
        private double fontSize = 10;
        public double FontSize
        {
            get => fontSize;
            set { fontSize = value; OnPropertyChanged(); }
        }

        // 字体加粗属性
        private bool isBold = false;
        public bool IsBold
        {
            get => isBold;
            set { isBold = value; OnPropertyChanged(); }
        }
    }
}
