using System;
using Avalonia;
using Avalonia.Controls;

namespace Grimoire.Controls;

/// <summary>
/// Lays children out in as many equal-width columns as fit, each at least <see cref="MinItemWidth"/> wide.
/// Gives two columns on a phone and five or six on a desktop window, with no ragged right edge.
/// </summary>
public class AdaptiveGrid : Panel
{
    public static readonly StyledProperty<double> MinItemWidthProperty =
        AvaloniaProperty.Register<AdaptiveGrid, double>(nameof(MinItemWidth), 150);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<AdaptiveGrid, double>(nameof(Spacing), 12);

    static AdaptiveGrid()
    {
        AffectsMeasure<AdaptiveGrid>(MinItemWidthProperty, SpacingProperty);
    }

    public double MinItemWidth
    {
        get => GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    private (int Columns, double ItemWidth) Layout(double width)
    {
        if (double.IsInfinity(width) || width <= 0) width = MinItemWidth;
        var columns = Math.Max(1, (int)((width + Spacing) / (MinItemWidth + Spacing)));
        var itemWidth = (width - Spacing * (columns - 1)) / columns;
        return (columns, Math.Max(0, itemWidth));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var (columns, itemWidth) = Layout(availableSize.Width);
        double height = 0, rowHeight = 0;
        var col = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(itemWidth, double.PositiveInfinity));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            if (++col == columns)
            {
                height += rowHeight + Spacing;
                rowHeight = 0;
                col = 0;
            }
        }
        if (col > 0) height += rowHeight;
        else if (height > 0) height -= Spacing;

        var width = double.IsInfinity(availableSize.Width) ? columns * itemWidth : availableSize.Width;
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (columns, itemWidth) = Layout(finalSize.Width);
        double y = 0, rowHeight = 0;
        var col = 0;

        // Row heights: use the tallest child in each row so cards line up.
        for (var start = 0; start < Children.Count; start += columns)
        {
            rowHeight = 0;
            for (var i = start; i < Math.Min(start + columns, Children.Count); i++)
                rowHeight = Math.Max(rowHeight, Children[i].DesiredSize.Height);

            col = 0;
            for (var i = start; i < Math.Min(start + columns, Children.Count); i++, col++)
                Children[i].Arrange(new Rect(col * (itemWidth + Spacing), y, itemWidth, rowHeight));

            y += rowHeight + Spacing;
        }
        return finalSize;
    }
}
