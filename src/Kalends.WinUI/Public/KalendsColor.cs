namespace Kalends;

/// <summary>Theme-independent RGB color carried by an event or calendar.</summary>
public readonly record struct KalendsColor(byte R, byte G, byte B, byte A = 255)
{
    public static KalendsColor FromRgb(byte r, byte g, byte b) => new(r, g, b);

    public uint ToArgb() => (uint)(A << 24 | R << 16 | G << 8 | B);
}
