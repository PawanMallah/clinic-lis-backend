namespace LIS.QcService.Helpers;

/// <summary>
/// Implements Westgard multi-rule QC system.
/// Rules evaluated: 1-2s (warning), 1-3s, 2-2s, R-4s, 4-1s, 10x
/// </summary>
public static class WestgardRules
{
    public static (string status, string? violation) Evaluate(
        decimal measuredValue, decimal mean, decimal sd, List<decimal> recentValues)
    {
        var sdIndex = sd != 0 ? (measuredValue - mean) / sd : 0;

        // 1-3s rule: Single value exceeds ±3 SD (OUT OF CONTROL)
        if (Math.Abs(sdIndex) > 3)
            return ("out_of_control", "1-3s");

        // 2-2s rule: Two consecutive values exceed ±2 SD in same direction
        if (recentValues.Count >= 1)
        {
            var prevSdIndex = sd != 0 ? (recentValues[0] - mean) / sd : 0;
            if ((sdIndex > 2 && prevSdIndex > 2) || (sdIndex < -2 && prevSdIndex < -2))
                return ("out_of_control", "2-2s");
        }

        // R-4s rule: Difference between two consecutive exceeds 4 SD
        if (recentValues.Count >= 1)
        {
            var prevSdIndex = sd != 0 ? (recentValues[0] - mean) / sd : 0;
            if (Math.Abs(sdIndex - prevSdIndex) > 4)
                return ("out_of_control", "R-4s");
        }

        // 4-1s rule: Four consecutive values exceed ±1 SD in same direction
        if (recentValues.Count >= 3)
        {
            var allAbove = sdIndex > 1 && recentValues.Take(3).All(v => sd != 0 && ((v - mean) / sd) > 1);
            var allBelow = sdIndex < -1 && recentValues.Take(3).All(v => sd != 0 && ((v - mean) / sd) < -1);
            if (allAbove || allBelow)
                return ("out_of_control", "4-1s");
        }

        // 10x rule: Ten consecutive values on same side of mean
        if (recentValues.Count >= 9)
        {
            var allPositive = sdIndex > 0 && recentValues.Take(9).All(v => v > mean);
            var allNegative = sdIndex < 0 && recentValues.Take(9).All(v => v < mean);
            if (allPositive || allNegative)
                return ("out_of_control", "10x");
        }

        // 1-2s rule: Single value exceeds ±2 SD (WARNING only)
        if (Math.Abs(sdIndex) > 2)
            return ("warning", "1-2s");

        return ("in_control", null);
    }
}
