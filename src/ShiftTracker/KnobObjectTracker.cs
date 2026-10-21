using OpenCvSharp;

namespace ShiftTracker;

/// <summary>
/// Tracks sparse visual features on the shifter knob itself with pyramidal
/// Lucas-Kanade optical flow. It never detects or stabilizes the background.
/// Initialize once by clicking the center of the knob in the live preview.
/// </summary>
internal sealed class KnobObjectTracker : IDisposable
{
    private const int MaxFeatures = 90;
    private const int MinTrackedFeatures = 5;
    private const int KnobRadius = 24;
    private Mat? previousGray;
    private Point2f[] points = [];
    private Point2f center;

    public bool IsInitialized => previousGray is not null && points.Length >= MinTrackedFeatures;
    public int FeatureCount { get; private set; }
    public Point2f? Center => IsInitialized ? center : null;
    public string Status { get; private set; } = "Click the knob to start tracking";

    public bool Initialize(Mat frame, Point2f knobCenter)
    {
        Reset();
        if (frame.Empty() || !Inside(frame, knobCenter))
        {
            Status = "Knob center is outside the camera image";
            return false;
        }

        center = knobCenter;
        previousGray = ToGray(frame);
        points = DetectKnobFeatures(previousGray, center);
        FeatureCount = points.Length;
        if (FeatureCount < MinTrackedFeatures)
        {
            Reset();
            Status = "Not enough visible knob detail · improve focus or lighting";
            return false;
        }
        Status = $"Knob locked · {FeatureCount} object features";
        return true;
    }

    public bool TryTrack(Mat frame, out Point2f position)
    {
        position = default;
        if (frame.Empty() || previousGray is null || points.Length < MinTrackedFeatures)
        {
            Status = "Knob not initialized · click the knob in the live image";
            return false;
        }

        using Mat gray = ToGray(frame);
        Point2f[] nextPoints = [];
        Cv2.CalcOpticalFlowPyrLK(
            previousGray, gray, points, ref nextPoints, out byte[] status,
            out float[] error, new OpenCvSharp.Size(21, 21), 3,
            new TermCriteria(CriteriaTypes.Count | CriteriaTypes.Eps, 18, 0.025),
            OpticalFlowFlags.None, 0.0001);

        var dx = new List<float>(points.Length);
        var dy = new List<float>(points.Length);
        var valid = new List<(Point2f Point, float Dx, float Dy)>(points.Length);
        for (int i = 0; i < Math.Min(status.Length, nextPoints.Length); i++)
        {
            if (status[i] == 0 || i >= error.Length || error[i] > 35 || !Inside(gray, nextPoints[i])) continue;
            float x = nextPoints[i].X - points[i].X;
            float y = nextPoints[i].Y - points[i].Y;
            if (Math.Abs(x) > 180 || Math.Abs(y) > 180) continue;
            dx.Add(x); dy.Add(y); valid.Add((nextPoints[i], x, y));
        }

        if (valid.Count < MinTrackedFeatures)
        {
            previousGray.Dispose();
            previousGray = gray.Clone();
            points = [];
            FeatureCount = 0;
            Status = $"Knob tracking lost · only {valid.Count} features survived; click the knob to reacquire";
            return false;
        }

        float medianX = Median(dx), medianY = Median(dy);
        var residuals = new List<float>(valid.Count);
        for (int i = 0; i < valid.Count; i++)
        {
            float rx = valid[i].Dx - medianX;
            float ry = valid[i].Dy - medianY;
            residuals.Add(MathF.Sqrt(rx * rx + ry * ry));
        }
        float spread = Median(residuals);
        if (!float.IsFinite(spread) || spread > 12)
        {
            Status = "Knob motion is ambiguous · hold briefly or click the knob to reacquire";
            return false;
        }

        center = new Point2f(center.X + medianX, center.Y + medianY);
        if (!Inside(gray, center))
        {
            Status = "Knob moved outside the camera image";
            return false;
        }

        previousGray.Dispose();
        previousGray = gray.Clone();
        points = DetectKnobFeatures(gray, center);
        FeatureCount = points.Length;
        position = center;
        if (FeatureCount < MinTrackedFeatures)
        {
            Status = $"Knob track weak · {FeatureCount} features; click the knob to reacquire";
            return false;
        }
        Status = $"Knob tracked · {FeatureCount} features · optical flow";
        return true;
    }

    private static Point2f[] DetectKnobFeatures(Mat gray, Point2f at)
    {
        if (gray.Width < KnobRadius * 2 || gray.Height < KnobRadius * 2) return [];
        using var mask = new Mat(gray.Size(), MatType.CV_8UC1, Scalar.All(0));
        Cv2.Circle(mask, new OpenCvSharp.Point((int)Math.Round(at.X), (int)Math.Round(at.Y)), KnobRadius, Scalar.All(255), -1);
        Point2f[] found = Cv2.GoodFeaturesToTrack(gray, MaxFeatures, 0.012, 3.0, mask, 3, true, 0.04);
        return found.Where(p =>
        {
            float dx = p.X - at.X, dy = p.Y - at.Y;
            return dx * dx + dy * dy <= KnobRadius * KnobRadius;
        }).ToArray();
    }

    private static Mat ToGray(Mat frame)
    {
        var gray = new Mat();
        if (frame.Channels() == 1) frame.CopyTo(gray);
        else Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
        return gray;
    }

    private static bool Inside(Mat image, Point2f p) =>
        p.X >= 0 && p.Y >= 0 && p.X < image.Width && p.Y < image.Height;

    private static float Median(List<float> values)
    {
        if (values.Count == 0) return float.NaN;
        values.Sort();
        int m = values.Count / 2;
        return values.Count % 2 == 1 ? values[m] : (values[m - 1] + values[m]) * .5f;
    }

    public void Reset()
    {
        previousGray?.Dispose();
        previousGray = null;
        points = [];
        FeatureCount = 0;
        center = default;
        Status = "Click the knob to start tracking";
    }

    public void Dispose() => Reset();
}
