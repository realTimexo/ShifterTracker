using OpenCvSharp;

namespace ShiftTracker;

/// <summary>
/// Estimates a small camera transform from stable features around the outside
/// of the shifter. The central mechanism is excluded from feature matching.
/// Matching is anchored to the calibration frame, so errors do not accumulate.
/// </summary>
internal sealed class MotionCompensator : IDisposable
{
    private const int MinimumMatches = 12;
    private const int MinimumInliers = 9;
    private const float MaximumDescriptorDistance = 68f;
    private const double RansacThresholdPx = 2.8;

    private readonly ORB orb = ORB.Create(700, 1.2f, 4, 15, 0, 2, ORBScoreType.Harris, 31, 10);
    private readonly Random random = new(0x51F7);
    private Mat? anchorDescriptors;
    private Mat? featureMask;
    private KeyPoint[] anchorPoints = [];
    private OpenCvSharp.Size anchorSize;

    public bool IsReady => anchorDescriptors is not null && !anchorDescriptors.Empty();
    public int AnchorFeatureCount => anchorPoints.Length;
    public int LastMatchCount { get; private set; }
    public int LastInlierCount { get; private set; }
    public double LastInlierRatio { get; private set; }
    public string LastStatus { get; private set; } = "No adaptive reference";

    public bool SetReference(Mat frame, out string message)
    {
        Reset();
        if (frame.Empty())
        {
            message = "The camera frame is empty.";
            return false;
        }

        anchorSize = frame.Size();
        using var gray = ToGray(frame);
        featureMask = CreateFeatureMask(gray.Size());
        var descriptors = new Mat();
        orb.DetectAndCompute(gray, featureMask, out anchorPoints, descriptors);
        if (descriptors.Empty() || anchorPoints.Length < MinimumMatches)
        {
            descriptors.Dispose();
            anchorPoints = [];
            message = "Not enough fixed background detail. Keep the shifter frame visible and adjust the camera.";
            return false;
        }

        anchorDescriptors = descriptors;
        LastStatus = $"Reference ready · {anchorPoints.Length} fixed features";
        message = LastStatus;
        return true;
    }

    /// <summary>Aligns a camera frame to the saved calibration reference.</summary>
    public bool TryAlign(Mat source, out Mat? aligned, out string message)
    {
        aligned = null;
        LastMatchCount = 0;
        LastInlierCount = 0;
        LastInlierRatio = 0;

        if (!IsReady || featureMask is null)
        {
            message = "Adaptive reference is not set.";
            LastStatus = message;
            return false;
        }
        if (source.Size() != anchorSize)
        {
            message = "Camera resolution changed; recalibrate the adaptive reference.";
            LastStatus = message;
            return false;
        }

        using var gray = ToGray(source);
        using var descriptors = new Mat();
        orb.DetectAndCompute(gray, featureMask, out KeyPoint[] currentPoints, descriptors);
        if (descriptors.Empty() || currentPoints.Length < MinimumMatches)
        {
            message = "Too few stable background features in this frame.";
            LastStatus = message;
            return false;
        }

        using var matcher = new BFMatcher(NormTypes.Hamming, crossCheck: true);
        DMatch[] matches = matcher.Match(anchorDescriptors!, descriptors)
            .Where(m => m.Distance <= MaximumDescriptorDistance)
            .OrderBy(m => m.Distance)
            .Take(120)
            .ToArray();
        LastMatchCount = matches.Length;
        if (matches.Length < MinimumMatches)
        {
            message = $"Only {matches.Length} background feature matches.";
            LastStatus = message;
            return false;
        }

        var pairs = matches
            .Select(m => new PointPair(currentPoints[m.TrainIdx].Pt, anchorPoints[m.QueryIdx].Pt))
            .ToArray();
        Transform? best = FindRobustTransform(pairs, out List<int> inliers);
        LastInlierCount = inliers.Count;
        LastInlierRatio = matches.Length == 0 ? 0 : (double)inliers.Count / matches.Length;
        if (best is null || inliers.Count < MinimumInliers || LastInlierRatio < 0.28)
        {
            message = $"Alignment uncertain · {inliers.Count}/{matches.Length} fixed features agree.";
            LastStatus = message;
            return false;
        }

        double scale = Math.Sqrt(best.Value.A * best.Value.A + best.Value.B * best.Value.B);
        double rotation = Math.Atan2(best.Value.B, best.Value.A) * 180.0 / Math.PI;
        double translation = Math.Sqrt(best.Value.Tx * best.Value.Tx + best.Value.Ty * best.Value.Ty);
        if (scale is < 0.82 or > 1.18 || Math.Abs(rotation) > 12 || translation > Math.Max(anchorSize.Width, anchorSize.Height) * 0.18)
        {
            message = $"Camera moved beyond the adaptive range · {translation:F0}px, {rotation:F1}°.";
            LastStatus = message;
            return false;
        }

        using var matrix = new Mat(2, 3, MatType.CV_32FC1);
        matrix.At<float>(0, 0) = (float)best.Value.A;
        matrix.At<float>(0, 1) = (float)-best.Value.B;
        matrix.At<float>(0, 2) = (float)best.Value.Tx;
        matrix.At<float>(1, 0) = (float)best.Value.B;
        matrix.At<float>(1, 1) = (float)best.Value.A;
        matrix.At<float>(1, 2) = (float)best.Value.Ty;

        aligned = new Mat();
        Cv2.WarpAffine(source, aligned, matrix, anchorSize, InterpolationFlags.Linear,
            BorderTypes.Reflect101);
        message = $"Aligned · {inliers.Count}/{matches.Length} features · {translation:F1}px · {rotation:F1}°";
        LastStatus = message;
        return true;
    }

    public void Reset()
    {
        anchorDescriptors?.Dispose();
        anchorDescriptors = null;
        featureMask?.Dispose();
        featureMask = null;
        anchorPoints = [];
        anchorSize = default;
        LastMatchCount = 0;
        LastInlierCount = 0;
        LastInlierRatio = 0;
        LastStatus = "No adaptive reference";
    }

    private Transform? FindRobustTransform(PointPair[] pairs, out List<int> bestInliers)
    {
        bestInliers = [];
        Transform? best = null;
        if (pairs.Length < 2) return null;

        int iterations = Math.Min(180, pairs.Length * 3);
        for (int i = 0; i < iterations; i++)
        {
            int first = random.Next(pairs.Length);
            int second = random.Next(pairs.Length);
            if (first == second) continue;
            if (!TryFitPair(pairs[first], pairs[second], out Transform model)) continue;

            var inliers = new List<int>(pairs.Length);
            for (int j = 0; j < pairs.Length; j++)
                if (Residual(model, pairs[j]) <= RansacThresholdPx)
                    inliers.Add(j);

            if (inliers.Count > bestInliers.Count)
            {
                bestInliers = inliers;
                best = model;
            }
        }

        if (bestInliers.Count >= MinimumInliers)
            best = FitLeastSquares(pairs, bestInliers);
        return best;
    }

    private static bool TryFitPair(PointPair p, PointPair q, out Transform transform)
    {
        double dx = q.Source.X - p.Source.X;
        double dy = q.Source.Y - p.Source.Y;
        double denominator = dx * dx + dy * dy;
        if (denominator < 16)
        {
            transform = default;
            return false;
        }

        double targetX = q.Target.X - p.Target.X;
        double targetY = q.Target.Y - p.Target.Y;
        double a = (dx * targetX + dy * targetY) / denominator;
        double b = (dx * targetY - dy * targetX) / denominator;
        double tx = p.Target.X - (a * p.Source.X - b * p.Source.Y);
        double ty = p.Target.Y - (b * p.Source.X + a * p.Source.Y);
        transform = new Transform(a, b, tx, ty);
        return double.IsFinite(a) && double.IsFinite(b) && double.IsFinite(tx) && double.IsFinite(ty);
    }

    private static Transform FitLeastSquares(PointPair[] pairs, IReadOnlyList<int> indices)
    {
        double sx = 0, sy = 0, tx = 0, ty = 0;
        foreach (int index in indices)
        {
            sx += pairs[index].Source.X;
            sy += pairs[index].Source.Y;
            tx += pairs[index].Target.X;
            ty += pairs[index].Target.Y;
        }
        double n = indices.Count;
        sx /= n; sy /= n; tx /= n; ty /= n;

        double denominator = 0, aNumerator = 0, bNumerator = 0;
        foreach (int index in indices)
        {
            double x = pairs[index].Source.X - sx;
            double y = pairs[index].Source.Y - sy;
            double u = pairs[index].Target.X - tx;
            double v = pairs[index].Target.Y - ty;
            denominator += x * x + y * y;
            aNumerator += x * u + y * v;
            bNumerator += x * v - y * u;
        }
        if (denominator < 1) return default;
        double a = aNumerator / denominator;
        double b = bNumerator / denominator;
        return new Transform(a, b, tx - a * sx + b * sy, ty - b * sx - a * sy);
    }

    private static double Residual(Transform t, PointPair p)
    {
        double x = t.A * p.Source.X - t.B * p.Source.Y + t.Tx - p.Target.X;
        double y = t.B * p.Source.X + t.A * p.Source.Y + t.Ty - p.Target.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private static Mat ToGray(Mat frame)
    {
        var gray = new Mat();
        if (frame.Channels() == 1) frame.CopyTo(gray);
        else Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
        return gray;
    }

    private static Mat CreateFeatureMask(OpenCvSharp.Size size)
    {
        var mask = new Mat(size, MatType.CV_8UC1, Scalar.All(255));
        // The knob and the moving central slide are excluded. The fixed outer
        // housing, upper/lower frame and side rails remain available as anchors.
        int left = (int)(size.Width * 0.23);
        int top = (int)(size.Height * 0.22);
        int width = (int)(size.Width * 0.54);
        int height = (int)(size.Height * 0.62);
        Cv2.Rectangle(mask, new Rect(left, top, width, height), Scalar.All(0), -1);
        return mask;
    }

    public void Dispose()
    {
        Reset();
        orb.Dispose();
    }

    private readonly record struct PointPair(Point2f Source, Point2f Target);
    private readonly record struct Transform(double A, double B, double Tx, double Ty);
}
