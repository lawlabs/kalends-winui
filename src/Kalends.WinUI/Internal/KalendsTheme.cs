using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Kalends;

internal static class KalendsTheme
{
    public static ResourceDictionary Create()
    {
        var dictionary = new ResourceDictionary();
        dictionary.ThemeDictionaries["Light"] = Palette(
            shell: 0xFFF5F6F8, surface: 0xFFFFFFFF, muted: 0xFF777E8C, line: 0xFFE9ECF1,
            subtle: 0xFFF3F4F7, brand: 0xFF347967, onBrand: 0xFFFFFFFF, today: 0xFFF1F8F5,
            weekend: 0xFFFAFBFD, text: 0xFF252D3A, now: 0xFFD65860);
        dictionary.ThemeDictionaries["Dark"] = Palette(
            shell: 0xFF1D2229, surface: 0xFF242A33, muted: 0xFFA0AAB8, line: 0xFF39424E,
            subtle: 0xFF2C343F, brand: 0xFF80C8AE, onBrand: 0xFF102A21, today: 0xFF263B37,
            weekend: 0xFF21262E, text: 0xFFEDF1F7, now: 0xFFFF929A);
        dictionary.ThemeDictionaries["HighContrast"] = HighContrast();
        return dictionary;
    }

    private static ResourceDictionary Palette(
        uint shell, uint surface, uint muted, uint line, uint subtle, uint brand, uint onBrand,
        uint today, uint weekend, uint text, uint now)
    {
        var dictionary = new ResourceDictionary();
        Add(dictionary, "Shell", shell);
        Add(dictionary, "Surface", surface);
        Add(dictionary, "Muted", muted);
        Add(dictionary, "Line", line);
        Add(dictionary, "SubtleLine", subtle);
        Add(dictionary, "Brand", brand);
        Add(dictionary, "Selection", (brand & 0x00FFFFFFu) | 0x40000000u);
        Add(dictionary, "OnBrand", onBrand);
        Add(dictionary, "Today", today);
        Add(dictionary, "Weekend", weekend);
        Add(dictionary, "WeekendText", muted);
        Add(dictionary, "DayOff", (now & 0x00FFFFFFu) | 0x18000000u);
        Add(dictionary, "DayOffText", now);
        Add(dictionary, "WorkingDayText", brand);
        Add(dictionary, "Text", text);
        Add(dictionary, "Now", now);
        return dictionary;
    }

    private static ResourceDictionary HighContrast()
    {
        var settings = new UISettings();
        Color window = settings.GetColorValue(UIColorType.Background);
        Color ink = settings.GetColorValue(UIColorType.Foreground);
        Color accent = settings.GetColorValue(UIColorType.Accent);
        Color onAccent = settings.GetColorValue(UIColorType.AccentLight3);
        var dictionary = new ResourceDictionary();
        Set(dictionary, "Shell", window);
        Set(dictionary, "Surface", window);
        Set(dictionary, "Muted", ink);
        Set(dictionary, "Line", ink);
        Set(dictionary, "SubtleLine", ink);
        Set(dictionary, "Brand", accent);
        Set(dictionary, "Selection", window);
        Set(dictionary, "OnBrand", onAccent);
        Set(dictionary, "Today", window);
        Set(dictionary, "Weekend", window);
        Set(dictionary, "WeekendText", ink);
        Set(dictionary, "DayOff", window);
        Set(dictionary, "DayOffText", ink);
        Set(dictionary, "WorkingDayText", ink);
        Set(dictionary, "Text", ink);
        Set(dictionary, "Now", accent);
        return dictionary;
    }

    private static void Add(ResourceDictionary dictionary, string name, uint argb)
    {
        Set(dictionary, name, Color.FromArgb(
            (byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb));
    }

    private static void Set(ResourceDictionary dictionary, string name, Color color)
    {
        dictionary[name + "Brush"] = new SolidColorBrush(color);
        dictionary["Kalends" + name + "Brush"] = dictionary[name + "Brush"];
    }
}
