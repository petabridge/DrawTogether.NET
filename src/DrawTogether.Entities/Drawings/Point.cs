namespace DrawTogether.Entities.Drawings;

/// <summary>
/// Represents a single point in a drawing.
/// </summary>
public readonly struct Point(double x, double y)
{
    public double X { get; } = x;
    public double Y { get; } = y;
}