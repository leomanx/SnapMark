using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnapMark.App;

/// <summary>Only accepts a drag from this exact button panel; ordinary clicks keep selecting tools.</summary>
public static class ReorderButtons
{
    private const string Format = "SnapMark.ToolButton";
    public static void Attach(Panel panel, Button button, Action changed)
    {
        Point? start = null;
        button.PreviewMouseLeftButtonDown += (_, e) => start = e.GetPosition(button);
        button.PreviewMouseLeftButtonUp += (_, _) => start = null;
        button.PreviewMouseMove += (_, e) =>
        {
            if (start is not { } origin || e.LeftButton != MouseButtonState.Pressed) return;
            var current = e.GetPosition(button);
            if (Math.Abs(current.X - origin.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(current.Y - origin.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            start = null; button.ReleaseMouseCapture();
            DragDrop.DoDragDrop(button, new DataObject(Format, button), DragDropEffects.Move); e.Handled = true;
        };
        button.AllowDrop = true;
        button.DragOver += (_, e) => { e.Effects = e.Data.GetData(Format) is Button source && ReferenceEquals(source.Parent, panel) ? DragDropEffects.Move : DragDropEffects.None; e.Handled = true; };
        button.Drop += (_, e) =>
        {
            if (e.Data.GetData(Format) is Button source && ReferenceEquals(source.Parent, panel)) { Move(panel, source, button); changed(); }
            e.Handled = true;
        };
        var menu = new ContextMenu();
        var previous = new MenuItem { Header = "Move earlier / เลื่อนไปก่อน" }; var next = new MenuItem { Header = "Move later / เลื่อนไปหลัง" };
        menu.Items.Add(previous); menu.Items.Add(next); button.ContextMenu = menu;
        void Step(int offset) { var index = panel.Children.IndexOf(button); var target = index + offset; if (target >= 0 && target < panel.Children.Count) { Move(panel, button, (Button)panel.Children[target]); changed(); } }
        previous.Click += (_, _) => Step(-1); next.Click += (_, _) => Step(1);
    }
    public static void Move(Panel panel, Button source, Button target)
    {
        if (source == target || source.Parent != panel || target.Parent != panel) return;
        var index = panel.Children.IndexOf(target); panel.Children.Remove(source); panel.Children.Insert(index, source);
    }
}
