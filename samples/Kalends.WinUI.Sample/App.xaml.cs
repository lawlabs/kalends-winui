using System.Globalization;
using Microsoft.UI.Xaml;

namespace Kalends_WinUI_Sample;

public partial class App : Application
{
    private Window? _window;

    static App() => UseEnglish();

    public App()
    {
        UseEnglish();
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        if (_window.Content is FrameworkElement root)
            root.Language = "en-US";
        _window.Activate();
    }

    private static void UseEnglish()
    {
        Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US";
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
    }
}
