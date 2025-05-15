using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Task_4.Views
{
    public partial class ReflectionView : Window
    {
        public ReflectionView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 