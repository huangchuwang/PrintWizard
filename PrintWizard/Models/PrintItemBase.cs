using System;
using System.ComponentModel;

namespace PrintWizard.Models
{
    public abstract class PrintItemBase : INotifyPropertyChanged
    {
        private double x;
        private double y;
        private double width;
        private double height;

        public Guid Id { get; } = Guid.NewGuid();

        public virtual double Width
        {
            get => width;
            set { width = value; OnPropertyChanged(); }
        }

        public virtual double Height
        {
            get => height;
            set { height = value; OnPropertyChanged(); }
        }

        // X/Y 现在是相对于【可打印区域】左上角的坐标
        public double X
        {
            get => x;
            set { x = value; OnPropertyChanged(); }
        }

        public double Y
        {
            get => y;
            set { y = value; OnPropertyChanged(); }
        }

        public abstract string ItemType { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
