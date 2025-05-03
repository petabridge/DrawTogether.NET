using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.TestKit.Xunit2;
using Akka.Util;
using DrawTogether.Actors.Local;
using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DrawTogether.Tests
{
    public class StrokeGapAnalyzerTests : TestKit
    {
        private readonly ITestOutputHelper _output;

        public StrokeGapAnalyzerTests(ITestOutputHelper output) : base()
        {
            _output = output;
        }

        [Fact]
        public async Task Should_Analyze_Gaps_Between_Strokes()
        {
            // Arrange
            var userId = new UserId("test-user");
            var drawingSessionId = new DrawingSessionId("test-session");
            var color = new Color("#FF0000");  // Red
            var strokeWidth = new GreaterThanZeroInteger(5);

            // Simulate points from a horizontal line drawn across the screen
            var points = new List<Point>();
            var random = new Random(123); // Fixed seed for reproducibility
            
            // Create 100 points in roughly a horizontal line with small variations
            for (int i = 0; i < 100; i++)
            {
                double x = i * 10; // Points are 10 pixels apart horizontally
                double y = 400 + random.NextDouble() * 10 - 5; // Small vertical variations
                points.Add(new Point(x, y));
            }

            // Create point batches to simulate Akka.Streams GroupedWithin behavior
            // This is the key part - we'll create batches of random sizes to simulate
            // how the stream might group points
            var batches = new List<List<LocalPaintProtocol.AddPointToConnectedStroke>>();
            int currentIndex = 0;
            
            while (currentIndex < points.Count)
            {
                // Random batch size between 1 and 10 points
                int batchSize = random.Next(1, Math.Min(10, points.Count - currentIndex + 1));
                var batch = new List<LocalPaintProtocol.AddPointToConnectedStroke>();
                
                for (int i = 0; i < batchSize && currentIndex < points.Count; i++)
                {
                    batch.Add(new LocalPaintProtocol.AddPointToConnectedStroke(
                        points[currentIndex],
                        drawingSessionId,
                        userId,
                        strokeWidth,
                        color));
                    currentIndex++;
                }
                
                batches.Add(batch);
            }
            
            _output.WriteLine($"Created {batches.Count} batches from {points.Count} points");
            
            // Process batches through the StrokeBuilder
            var strokes = new List<ConnectedStroke>();
            int strokeIdCounter = 0;
            
            foreach (var batch in batches)
            {
                var batchStrokes = StrokeBuilder.ComputeStrokes(
                    batch, 
                    null, // No logger needed
                    _ => new StrokeId(strokeIdCounter++));
                
                strokes.AddRange(batchStrokes);
            }
            
            _output.WriteLine($"Produced {strokes.Count} stroke objects");
            
            // Analyze gaps between strokes
            AnalyzeStrokes(strokes, points);
            
            // Try various GroupedWithin parameters and analyze the results
            await TestStreamProcessing(points, userId, drawingSessionId, strokeWidth, color, 5, 30);
            await TestStreamProcessing(points, userId, drawingSessionId, strokeWidth, color, 10, 75);
            await TestStreamProcessing(points, userId, drawingSessionId, strokeWidth, color, 15, 100);
            await TestStreamProcessing(points, userId, drawingSessionId, strokeWidth, color, 20, 150);
        }
        
        private async Task TestStreamProcessing(
            List<Point> points,
            UserId userId,
            DrawingSessionId drawingSessionId,
            GreaterThanZeroInteger strokeWidth,
            Color color,
            int groupSize,
            int groupTimeMs)
        {
            _output.WriteLine($"\nTesting with GroupedWithin({groupSize}, {groupTimeMs}ms)");
            
            // Create source points
            var sourcePoints = points.Select(p => 
                new LocalPaintProtocol.AddPointToConnectedStroke(
                    p, drawingSessionId, userId, strokeWidth, color)).ToList();
            
            // Create a materializer
            var materializer = Sys.Materializer();
            
            // Setup stream with specified parameters
            var strokeIdCounter = 0;
            var resultStrokes = new List<ConnectedStroke>();
            
            await Source.From(sourcePoints)
                .GroupedWithin(groupSize, TimeSpan.FromMilliseconds(groupTimeMs))
                .Select(batch => StrokeBuilder.ComputeStrokes(
                    batch.ToList(),
                    null,
                    _ => new StrokeId(strokeIdCounter++)))
                .SelectMany(s => s)
                .RunForeach(stroke => resultStrokes.Add(stroke), materializer);
            
            _output.WriteLine($"Produced {resultStrokes.Count} stroke objects with GroupedWithin({groupSize}, {groupTimeMs}ms)");
            AnalyzeStrokes(resultStrokes, points);
        }
        
        private void AnalyzeStrokes(List<ConnectedStroke> strokes, List<Point> originalPoints)
        {
            if (strokes.Count == 0)
            {
                _output.WriteLine("No strokes to analyze");
                return;
            }
            
            // Calculate total points in all strokes
            int totalStrokePoints = strokes.Sum(s => s.Points.Count);
            _output.WriteLine($"Total points in strokes: {totalStrokePoints} (original: {originalPoints.Count})");
            
            // Calculate gaps between strokes
            var gaps = new List<double>();
            
            for (int i = 0; i < strokes.Count - 1; i++)
            {
                var currentStroke = strokes[i];
                var nextStroke = strokes[i + 1];
                
                if (currentStroke.Points.Count > 0 && nextStroke.Points.Count > 0)
                {
                    var lastPointInCurrent = currentStroke.Points[currentStroke.Points.Count - 1];
                    var firstPointInNext = nextStroke.Points[0];
                    
                    double distance = CalculateDistance(lastPointInCurrent, firstPointInNext);
                    gaps.Add(distance);
                }
            }
            
            if (gaps.Count > 0)
            {
                _output.WriteLine($"Gaps between strokes: {gaps.Count}");
                _output.WriteLine($"Min gap: {gaps.Min():F2} pixels");
                _output.WriteLine($"Max gap: {gaps.Max():F2} pixels");
                _output.WriteLine($"Average gap: {gaps.Average():F2} pixels");
                
                // Count significant gaps (more than 5 pixels)
                int significantGaps = gaps.Count(g => g > 5);
                _output.WriteLine($"Gaps > 5px: {significantGaps} ({(double)significantGaps / gaps.Count:P1} of all gaps)");
                
                // Histogram of gap sizes
                var histogram = new Dictionary<string, int>
                {
                    {"0-5", 0},
                    {"5-10", 0},
                    {"10-20", 0},
                    {"20-50", 0},
                    {"50+", 0}
                };
                
                foreach (var gap in gaps)
                {
                    if (gap <= 5) histogram["0-5"]++;
                    else if (gap <= 10) histogram["5-10"]++;
                    else if (gap <= 20) histogram["10-20"]++;
                    else if (gap <= 50) histogram["20-50"]++;
                    else histogram["50+"]++;
                }
                
                _output.WriteLine("Gap histogram:");
                foreach (var entry in histogram)
                {
                    _output.WriteLine($"  {entry.Key}: {entry.Value} ({(double)entry.Value / gaps.Count:P1})");
                }
            }
            else
            {
                _output.WriteLine("No gaps between strokes found");
            }
        }
        
        private double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
    }
} 