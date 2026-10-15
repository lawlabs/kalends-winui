using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Kalends;

public sealed partial class KalendsView
{
    private (FrameworkElement Element, DateTime Day)? CompactDropTarget(Point point)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= _root.ActualWidth || point.Y >= _root.ActualHeight) return null;
        foreach (var (target, day) in _dateTargets)
        {
            var corner = target.TransformToVisual(_root).TransformPoint(new Point());
            if (point.X >= corner.X && point.X < corner.X + target.ActualWidth &&
                point.Y >= corner.Y && point.Y < corner.Y + target.ActualHeight)
                return (target, day);
        }
        return null;
    }

    private void ConfigureCompactGesture(EventTile tile, IKalendsEvent item)
    {
        Point origin = default, grabOffset = default;
        DateTime grabbedDate = item.Start.Date;
        bool active = false, moved = false;
        Canvas? layer = null;
        Border? preview = null, destination = null;

        void Cleanup()
        {
            active = false;
            _gesture = false;
            _cancelGesture = null;
            tile.Opacity = 1;
            tile.ReleasePointerCaptures();
            if (layer != null) _root.Children.Remove(layer);
            layer = null;
            preview = destination = null;
            // Apply changes deferred while the pointer was captured, including window resizing.
            if (_refreshPending)
                DispatcherQueue.TryEnqueue(() => { if (IsLoaded && !_gesture) Refresh(); });
        }

        void Cancel()
        {
            if (!active) return;
            Cleanup();
            StatusChanged?.Invoke(this, Strings.DragCancelled);
        }

        tile.Press = e =>
        {
            if (_gesture || !e.GetCurrentPoint(tile).Properties.IsLeftButtonPressed || !tile.CapturePointer(e.Pointer)) return;
            origin = e.GetCurrentPoint(_root).Position;
            grabOffset = e.GetCurrentPoint(tile).Position;
            grabbedDate = CompactDropTarget(origin)?.Day ?? item.Start.Date;
            active = true;
            moved = false;
            _gesture = true;
            _cancelGesture = Cancel;
            tile.Focus(FocusState.Pointer);
            e.Handled = true;
        };
        tile.Move = e =>
        {
            tile.SetCursor(InputSystemCursorShape.SizeAll);
            if (!active) return;
            var point = e.GetCurrentPoint(_root).Position;
            if (!moved && Math.Abs(point.X - origin.X) + Math.Abs(point.Y - origin.Y) < 5) return;
            moved = true;
            tile.Opacity = .45;
            var target = CompactDropTarget(point);
            if (layer == null)
            {
                layer = new Canvas { IsHitTestVisible = false };
                Grid.SetRowSpan(layer, Math.Max(1, _root.RowDefinitions.Count));
                Canvas.SetZIndex(layer, 30);
                destination = new Border
                {
                    BorderBrush = EventInk(item.Color),
                    BorderThickness = new Thickness(2),
                };
                var card = new Grid();
                card.Children.Add(new Border
                {
                    Background = EventFill(item.Color),
                    BorderBrush = EventInk(item.Color),
                    BorderThickness = tile.BorderThickness,
                    CornerRadius = tile.CornerRadius,
                    Padding = tile.Padding,
                    Child = ColoredText((item.IsAllDay ? "" : $"{FormatTime(item.Start)}  ") + item.Title,
                        EventInk(item.Color), 11, true),
                });
                card.Children.Add(new Border
                {
                    BorderBrush = EventInk(item.Color),
                    BorderThickness = new Thickness(2),
                    CornerRadius = tile.CornerRadius,
                });
                preview = new Border
                {
                    Background = Brush("Surface"),
                    CornerRadius = tile.CornerRadius,
                    Child = card,
                };
                layer.Children.Add(destination);
                layer.Children.Add(preview);
                _root.Children.Add(layer);
            }
            layer.Clip = new RectangleGeometry { Rect = new Rect(0, 0, _root.ActualWidth, _root.ActualHeight) };
            // Pointer coordinates and the grab offset are both logical XAML coordinates.
            Canvas.SetLeft(preview!, point.X - grabOffset.X);
            Canvas.SetTop(preview!, point.Y - grabOffset.Y);
            preview!.Width = tile.ActualWidth;
            preview.Height = tile.ActualHeight;
            destination!.Visibility = target.HasValue ? Visibility.Visible : Visibility.Collapsed;
            if (target is { } found)
            {
                var corner = found.Element.TransformToVisual(_root).TransformPoint(new Point());
                Canvas.SetLeft(destination, corner.X);
                Canvas.SetTop(destination, corner.Y);
                destination.Width = found.Element.ActualWidth;
                destination.Height = found.Element.ActualHeight;
            }
            StatusChanged?.Invoke(this, target.HasValue
                ? Strings.FormatDragToDate(target.Value.Day.ToString("d", Culture), Culture)
                : Strings.DragToDate);
            e.Handled = true;
        };
        tile.Release = e =>
        {
            if (!active) return;
            var target = CompactDropTarget(e.GetCurrentPoint(_root).Position);
            Cleanup();
            if (moved && target is { } found)
            {
                var (start, end) = CompactDrag.MoveToDate(item, grabbedDate, found.Day);
                if (start != item.Start) RequestTime(item, start, end);
            }
            else if (!moved)
            {
                tile.Focus(FocusState.Programmatic);
                SelectEvent(item);
            }
            else StatusChanged?.Invoke(this, Strings.DragCancelled);
            e.Handled = true;
        };
        tile.PointerCanceled += (_, _) => Cancel();
        tile.PointerCaptureLost += (_, _) => Cancel();
    }
}
