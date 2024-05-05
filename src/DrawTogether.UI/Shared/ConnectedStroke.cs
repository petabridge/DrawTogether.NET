namespace DrawTogether.UI.Shared;

public class ConnectedStroke
{
    public Guid Id { get; set; }

    public List<Point> Points { get; set; } = new();

    public string Stroke { get; set; }

    public int StrokeWidth { get; set; }
}