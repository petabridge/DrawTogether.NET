namespace DrawTogether.UI.Shared;

public readonly struct Point(double x, double y)
{
    public double X { get; } = x;
    public double Y { get; } = y;
}