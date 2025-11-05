using LiveChartsCore.Defaults;
using SkiaSharp;

namespace CommandProjectPV_425_Test.Helpers;

// Расширяем ObservablePoint, чтобы добавить свойство цвета
public class ColoredPoint : ObservablePoint
{
    public SKColor Color { get; set; }

    public ColoredPoint(double x, double y, SKColor color) : base(x, y)
    {
        Color = color;
    }
}
