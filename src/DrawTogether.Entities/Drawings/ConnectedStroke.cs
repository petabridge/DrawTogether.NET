﻿namespace DrawTogether.Entities.Drawings;

public readonly struct StrokeId(int id) : IEquatable<StrokeId>
{
    public int Id { get; } = id;
    // override the == operator
    public static bool operator ==(StrokeId left, StrokeId right) => left.Id.Equals(right.Id);

    public static bool operator !=(StrokeId left, StrokeId right) => !(left == right);

    public bool Equals(StrokeId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is StrokeId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }
}

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
    public IReadOnlyList<Point> Points { get; init; } = Array.Empty<Point>();
    
    public GreaterThanZeroInteger StrokeWidth { get; init; } = GreaterThanZeroInteger.Default;
    
    public Color StrokeColor { get; init; } = Color.Black;

    public bool Equals(ConnectedStroke? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return StrokeWidth.Equals(other.StrokeWidth) && StrokeColor.Equals(other.StrokeColor) && Id.Equals(other.Id) && Points.SequenceEqual(other.Points);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Points, StrokeWidth, StrokeColor, Id);
    }
}