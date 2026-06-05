namespace Shared.Helpers;

public static class BarcodeHelper
{
    /// <summary>
    /// Generates a unique barcode in format: LIS-YYMMDD-XXXXX
    /// e.g., LIS-260604-00001
    /// </summary>
    /// <param name="sequence">The sequence number for today</param>
    /// <returns>A formatted barcode string</returns>
    public static string Generate(int sequence)
    {
        var datePart = DateTime.UtcNow.ToString("yyMMdd");
        return $"LIS-{datePart}-{sequence:D5}";
    }

    /// <summary>
    /// Generates a barcode with a random 5-digit suffix (fallback when sequence is unavailable)
    /// </summary>
    /// <returns>A formatted barcode string with random suffix</returns>
    public static string GenerateRandom()
    {
        var datePart = DateTime.UtcNow.ToString("yyMMdd");
        var random = Random.Shared.Next(10000, 99999);
        return $"LIS-{datePart}-{random:D5}";
    }
}
