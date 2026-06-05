namespace LIS.ResultService.Helpers;

public static class ResultFlagHelper
{
    public static string EvaluateFlag(decimal? numericValue, decimal? refLow, decimal? refHigh, decimal? critLow, decimal? critHigh)
    {
        if (!numericValue.HasValue) return "normal";
        var val = numericValue.Value;
        if (critLow.HasValue && val < critLow.Value) return "critical_low";
        if (critHigh.HasValue && val > critHigh.Value) return "critical_high";
        if (refLow.HasValue && val < refLow.Value) return "low";
        if (refHigh.HasValue && val > refHigh.Value) return "high";
        return "normal";
    }

    public static bool IsCriticalFlag(string flag)
    {
        return flag == "critical_low" || flag == "critical_high";
    }
}
