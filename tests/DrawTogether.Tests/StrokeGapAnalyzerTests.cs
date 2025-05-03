using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.TestKit.Xunit2;
using DrawTogether.Actors.Local;
using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;
using System.Collections.Immutable;
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
            
            // Create points with more realistic spacing to simulate actual mouse movement
            // A typical mouse movement would have points much closer together than 10px
            double currentX = 0;
            double currentY = 400;
            
            // Generate 100 points with more realistic spacing (1-3px between points)
            for (var i = 0; i < 100; i++)
            {
                // Small random movement in x direction (1-3 pixels)
                currentX += 1 + random.NextDouble() * 2;
                
                // Small random movement in y direction (-1 to +1 pixel)
                currentY += random.NextDouble() * 2 - 1;
                
                points.Add(new Point(currentX, currentY));
            }

            _output.WriteLine($"Generated 100 points with realistic spacing - total distance: {CalculateDistance(points.First(), points.Last()):F2} pixels");

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
            AnalyzeStrokes(strokes.ToImmutableList(), points);
            
            // Try various GroupedWithin parameters and analyze the results using the production code
            await TestStreamProcessingWithProductionCode(points, userId, drawingSessionId, strokeWidth, color, 5, 30);
            await TestStreamProcessingWithProductionCode(points, userId, drawingSessionId, strokeWidth, color, 10, 75);
            await TestStreamProcessingWithProductionCode(points, userId, drawingSessionId, strokeWidth, color, 15, 100);
            await TestStreamProcessingWithProductionCode(points, userId, drawingSessionId, strokeWidth, color, 20, 150);
        }
        
        private async Task TestStreamProcessingWithProductionCode(
            List<Point> points,
            UserId userId,
            DrawingSessionId drawingSessionId,
            GreaterThanZeroInteger strokeWidth,
            Color color,
            int groupSize,
            int groupTimeMs)
        {
            _output.WriteLine($"\nTesting with GroupedWithin({groupSize}, {groupTimeMs}ms) using production code");
            
            // Create source points
            var sourcePoints = points.Select(p => 
                new LocalPaintProtocol.AddPointToConnectedStroke(
                    p, drawingSessionId, userId, strokeWidth, color)).ToList();
            
            // Create a materializer
            var materializer = Sys.Materializer();
            
            // Create the input source
            var inputSource = Source.From(sourcePoints);
            
            // Use the production StrokeBuilder.CreateStrokeSource method
            var strokeCommandSource = StrokeBuilder.CreateStrokeSource(
                inputSource, 
                null, // No logger needed
                drawingSessionId,
                TimeSpan.FromMilliseconds(groupTimeMs),
                groupSize);
                
            // Collect the results
            var sink = Sink.Seq<ConnectedStroke>();
            
            var resultStrokes = await strokeCommandSource
                .Select(cmd => (cmd as DrawingSessionCommands.AddStroke)?.Stroke)
                .Where(stroke => stroke != null)
                .Select(c => c!)
                .RunWith(sink, materializer);
            
            _output.WriteLine($"Produced {resultStrokes.Count} stroke objects with GroupedWithin({groupSize}, {groupTimeMs}ms)");
            AnalyzeStrokes(resultStrokes, points);
        }
        
        private void AnalyzeStrokes(IImmutableList<ConnectedStroke> strokes, List<Point> originalPoints)
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
                    var lastPointInCurrent = currentStroke.Points[^1];
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
        
        private static double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        [Fact]
        public void Should_Create_Single_Stroke_With_Single_Batch()
        {
            // Arrange
            var userId = new UserId("test-user");
            var drawingSessionId = new DrawingSessionId("test-session");
            var color = new Color("#FF0000");  // Red
            var strokeWidth = new GreaterThanZeroInteger(5);

            // Simulate points from a horizontal line drawn across the screen
            var points = new List<Point>();
            var random = new Random(123); // Fixed seed for reproducibility
            
            // Create points with realistic spacing
            double currentX = 0;
            double currentY = 400;
            
            // Generate 100 points with realistic spacing (1-3px between points)
            for (var i = 0; i < 100; i++)
            {
                currentX += 1 + random.NextDouble() * 2;
                currentY += random.NextDouble() * 2 - 1;
                points.Add(new Point(currentX, currentY));
            }

            _output.WriteLine($"Generated {points.Count} points with realistic spacing");
            
            // Create a single batch with all points
            var allPointsBatch = points.Select(p => 
                new LocalPaintProtocol.AddPointToConnectedStroke(
                    p, drawingSessionId, userId, strokeWidth, color)).ToList();
            
            // Process with original StrokeBuilder logic (no batching)
            int strokeIdCounter = 0;
            var strokes = StrokeBuilder.ComputeStrokes(
                allPointsBatch,
                null, // No logger needed
                _ => new StrokeId(strokeIdCounter++)).ToList();
            
            // Assert
            _output.WriteLine($"Number of strokes created: {strokes.Count}");
            Assert.Single(strokes); // Should be exactly one stroke
            
            var stroke = strokes[0];
            _output.WriteLine($"Stroke points: {stroke.Points.Count} (original: {points.Count})");
            Assert.Equal(points.Count, stroke.Points.Count);
            
            // Verify the first and last points match
            Assert.Equal(points.First().X, stroke.Points.First().X);
            Assert.Equal(points.First().Y, stroke.Points.First().Y);
            Assert.Equal(points.Last().X, stroke.Points.Last().X);
            Assert.Equal(points.Last().Y, stroke.Points.Last().Y);
            
            _output.WriteLine("All points correctly included in a single stroke with no gaps");
        }

        [Fact]
        public async Task Should_Maintain_Stroke_Continuity_Across_Batches()
        {
            // Arrange
            var userId = new UserId("test-user");
            var drawingSessionId = new DrawingSessionId("test-session");
            var color = new Color("#FF0000");  // Red
            var strokeWidth = new GreaterThanZeroInteger(5);

            // Simulate points from a horizontal line drawn across the screen
            var points = new List<Point>();
            var random = new Random(123); // Fixed seed for reproducibility
            
            // Create points with realistic spacing
            double currentX = 0;
            double currentY = 400;
            
            // Generate 100 points with realistic spacing (1-3px between points)
            for (var i = 0; i < 100; i++)
            {
                currentX += 1 + random.NextDouble() * 2;
                currentY += random.NextDouble() * 2 - 1;
                points.Add(new Point(currentX, currentY));
            }

            _output.WriteLine($"Generated {points.Count} points with realistic spacing");
            
            // Create multiple batches to simulate Akka.Streams GroupedWithin
            var batches = new List<ImmutableList<LocalPaintProtocol.AddPointToConnectedStroke>>();
            int currentIndex = 0;
            int batchSize = 10; // Fixed batch size for predictability
            
            while (currentIndex < points.Count)
            {
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
                
                batches.Add(batch.ToImmutableList());
            }
            
            _output.WriteLine($"Created {batches.Count} batches from {points.Count} points");
            
            // Create a source from the batches
            var materializer = Sys.Materializer();
            var source = Source.From(batches);
            
            // Use the StrokeContinuityStage directly
            var strokeContinuityStage = new StrokeContinuityStage(TimeSpan.FromMilliseconds(1000));
            var sink = Sink.Seq<ConnectedStroke>();
            
            var resultStrokes = await source
                .Via(strokeContinuityStage)
                .SelectMany(strokes => strokes)
                .RunWith(sink, materializer);
            
            // Assert
            _output.WriteLine($"Number of strokes created with continuity stage: {resultStrokes.Count}");
            
            // Analyze the gaps between strokes
            AnalyzeStrokes(resultStrokes.ToImmutableList(), points);
            
            // Verify that strokes are continuous (no significant gaps)
            if (resultStrokes.Count > 1)
            {
                var gaps = new List<double>();
                
                for (int i = 0; i < resultStrokes.Count - 1; i++)
                {
                    var currentStroke = resultStrokes[i];
                    var nextStroke = resultStrokes[i + 1];
                    
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
                    _output.WriteLine($"Found {gaps.Count} gaps between consecutive strokes");
                    _output.WriteLine($"Min gap: {gaps.Min():F2} pixels");
                    _output.WriteLine($"Max gap: {gaps.Max():F2} pixels");
                    _output.WriteLine($"Average gap: {gaps.Average():F2} pixels");
                    
                    // Check that all gaps are very small (less than 3 pixels)
                    // This is the key assertion - we want to ensure the strokes connect visually
                    Assert.True(gaps.Max() < 3.0, 
                        $"Gaps between strokes should be minimal (< 3px) but found max gap of {gaps.Max():F2}px");
                }
                else
                {
                    _output.WriteLine("No measurable gaps between strokes (perfect continuity)");
                }
            }
            
            // Count total points across all strokes
            int totalPoints = resultStrokes.Sum(s => s.Points.Count);
            _output.WriteLine($"Total points in all strokes: {totalPoints} (original: {points.Count})");
            Assert.Equal(points.Count, totalPoints);
            
            _output.WriteLine("StrokeContinuityStage maintained visual continuity across batches");
        }
    }
} 