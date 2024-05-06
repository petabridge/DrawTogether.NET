namespace DrawTogether.Entities.Drawings;

public readonly struct StrokeId(int Id);

public sealed record Color(string HexCodeOrColorName)
{
    public static readonly Color Black = new("#000000");
}

/// <summary>
/// Represents a completed brush stroke in the SVG drawing.
/// </summary>
/// <param name="Id">The unique identity for this stroke.</param>
public sealed record ConnectedStroke(StrokeId Id)
{
    public IReadOnlyCollection<Point> Points { get; init; } = new List<Point>();
    
    public GreaterThanZeroInteger StrokeWidth { get; init; } = GreaterThanZeroInteger.Default;
    
    public Color StrokeColor { get; init; } = Color.Black;
}

public sealed class DrawingState
{
    
}