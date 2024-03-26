using DevToys.Api;
using DevToys.Blazor.Core.Services;

namespace DevToys.Web.Services;

[Export(typeof(IThemeListener))]
public class ThemeListener : IThemeListener
{
    [ImportingConstructor]
    public ThemeListener(ISettingsProvider settingsProvider)
    {

    }
    public AvailableApplicationTheme CurrentSystemTheme => AvailableApplicationTheme.Light;

    public AvailableApplicationTheme CurrentAppTheme => AvailableApplicationTheme.Light;

    public ApplicationTheme ActualAppTheme => ApplicationTheme.Light;

    public bool IsHighContrast => false;

    public bool IsCompactMode => false;

    public bool UserIsCompactModePreference => false;

    public event EventHandler? ThemeChanged;

    public void ApplyDesiredColorTheme()
    {
    }
}
