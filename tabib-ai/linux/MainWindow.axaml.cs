using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TabibAI.Linux.Ui.Chat;

namespace TabibAI.Linux;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services?.GetRequiredService<AppViewModel>();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
