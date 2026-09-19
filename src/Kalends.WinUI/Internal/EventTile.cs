using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Kalends;

internal sealed class EventTile : Button
{
    internal Action<PointerRoutedEventArgs>? Press;
    internal Action<PointerRoutedEventArgs>? Move;
    internal Action<PointerRoutedEventArgs>? Release;
    private static readonly InputSystemCursor MoveCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
    private static readonly InputSystemCursor ResizeCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);

    internal void SetCursor(InputSystemCursorShape shape) =>
        ProtectedCursor = shape == InputSystemCursorShape.SizeAll ? MoveCursor : ResizeCursor;

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        if (Press != null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) Press(e);
        else base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        if (Move != null) Move(e);
        else base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        if (Release != null) Release(e);
        else base.OnPointerReleased(e);
    }
}
