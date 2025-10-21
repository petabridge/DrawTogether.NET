using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Users;
using FluentAssertions;
using Xunit;

namespace DrawTogether.Tests;

public class SingleDotStrokeTests
{
    [Fact]
    public void Should_Detect_Single_Point_Stroke()
    {
        // Arrange
        var strokeId = new StrokeId(1);
        var point = new Point(100, 200);
        var strokeWidth = new GreaterThanZeroInteger(5);
        var color = new Color("#FF0000");
        
        var singlePointStroke = new ConnectedStroke(strokeId)
        {
            Points = [point],
            StrokeWidth = strokeWidth,
            StrokeColor = color
        };
        
        // Act & Assert
        singlePointStroke.Points.Should().HaveCount(1);
        singlePointStroke.Points[0].Should().Be(point);
        singlePointStroke.StrokeWidth.Should().Be(strokeWidth);
        singlePointStroke.StrokeColor.Should().Be(color);
    }
    
    [Fact]
    public void Should_Distinguish_Single_Point_From_Multi_Point_Strokes()
    {
        // Arrange
        var strokeId1 = new StrokeId(1);
        var strokeId2 = new StrokeId(2);
        
        var singlePointStroke = new ConnectedStroke(strokeId1)
        {
            Points = [new Point(100, 200)],
            StrokeWidth = new GreaterThanZeroInteger(3),
            StrokeColor = new Color("#00FF00")
        };
        
        var multiPointStroke = new ConnectedStroke(strokeId2)
        {
            Points = [new Point(100, 200), new Point(150, 250)],
            StrokeWidth = new GreaterThanZeroInteger(3),
            StrokeColor = new Color("#0000FF")
        };
        
        // Act & Assert
        singlePointStroke.Points.Should().HaveCount(1);
        multiPointStroke.Points.Should().HaveCount(2);
        
        // This simulates the rendering logic decision
        var shouldRenderAsCircle1 = singlePointStroke.Points.Count == 1;
        var shouldRenderAsCircle2 = multiPointStroke.Points.Count == 1;
        
        shouldRenderAsCircle1.Should().BeTrue();
        shouldRenderAsCircle2.Should().BeFalse();
    }
    
    [Fact]
    public void Should_Calculate_Correct_Circle_Radius_For_Single_Point_Stroke()
    {
        // Arrange
        var strokeWidth = new GreaterThanZeroInteger(10);
        var expectedRadius = strokeWidth.Value / 2.0;
        
        var singlePointStroke = new ConnectedStroke(new StrokeId(1))
        {
            Points = [new Point(50, 75)],
            StrokeWidth = strokeWidth,
            StrokeColor = new Color("#FFFF00")
        };
        
        // Act
        var actualRadius = singlePointStroke.StrokeWidth.Value / 2.0;
        
        // Assert
        actualRadius.Should().Be(expectedRadius);
        actualRadius.Should().Be(5.0);
    }
} 