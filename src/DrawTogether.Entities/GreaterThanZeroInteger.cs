namespace DrawTogether.Entities;

public readonly struct GreaterThanZeroInteger
{
    public GreaterThanZeroInteger() : this(1){}
    
    public GreaterThanZeroInteger(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentException("Value must be non-zero.", nameof(value));
        }
        Value = value;
    }

    public int Value { get; }
    
    public static GreaterThanZeroInteger Default => new();
}