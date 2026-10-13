using System.Collections.Generic;
using System.Linq;

public static class IndicatorLayoutSolver
{
    public static List<(AppleDirectionIndicator indicator, float finalAngle)> Resolve(
        List<(AppleDirectionIndicator indicator, float angle)> visible,
        float clusterThreshold,
        float angularStep)
    {
        if (visible.Count == 0) return new();

        var sorted = visible
            .Select(v => (v.indicator, angle: ((v.angle % 360) + 360) % 360))
            .OrderBy(v => v.angle)
            .ToList();

        // Find the biggest gap between neighbors (with wraparound) and "cut" there.
        // This turns the circle into a line so 359°/1° don't get treated as far apart.
        int cutIndex = 0;
        float largestGap = -1f;
        for (int i = 0; i < sorted.Count; i++)
        {
            float gap = (sorted[(i + 1) % sorted.Count].angle - sorted[i].angle + 360f) % 360f;
            if (gap > largestGap) { largestGap = gap; cutIndex = (i + 1) % sorted.Count; }
        }

        var ordered = sorted.Skip(cutIndex).Concat(sorted.Take(cutIndex)).ToList();

        // Unwrap past the seam so angles increase monotonically — no more wraparound to handle
        for (int i = 1; i < ordered.Count; i++)
            while (ordered[i].angle < ordered[i - 1].angle)
                ordered[i] = (ordered[i].indicator, ordered[i].angle + 360f);

        // Group adjacent entries within clusterThreshold of each other
        var clusters = new List<List<(AppleDirectionIndicator, float)>> { new() { ordered[0] } };
        for (int i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].angle - clusters[^1][^1].Item2 <= clusterThreshold)
                clusters[^1].Add(ordered[i]);
            else
                clusters.Add(new() { ordered[i] });
        }

        // Spread each cluster evenly around its mean, angularStep degrees apart
        var result = new List<(AppleDirectionIndicator, float)>();
        foreach (var cluster in clusters)
        {
            float mean = cluster.Average(c => c.Item2);
            float start = mean - angularStep * (cluster.Count - 1) / 2f;
            for (int i = 0; i < cluster.Count; i++)
            {
                float a = (start + i * angularStep + 360f) % 360f;
                result.Add((cluster[i].Item1, a));
            }
        }
        return result;
    }
}