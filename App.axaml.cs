using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Task_4.ViewModels;
using Task_4.Views;

namespace Task_4;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new ReflectionView
            {
                DataContext = new ReflectionViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}