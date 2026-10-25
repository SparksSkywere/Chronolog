namespace Chronolog;

/// <summary>
/// Named product SKU for this binary. Different OS families need different runtimes.
/// </summary>
public static class AppEdition
{
#if NETFRAMEWORK
    public const string Id = "classic";
    public const string ProductName = "Chronolog Classic";
    public const string ShortName = "Classic";
    public const string SupportedOsLabel = "Windows 7 SP1, Windows 8, Windows 8.1 (also runs on 10/11)";
    public const string RuntimeLabel = ".NET Framework 4.8";
    public const string MinWindowsVersion = "6.1"; // Win7
#else
    public const string Id = "modern";
    public const string ProductName = "Chronolog Modern";
    public const string ShortName = "Modern";
    public const string SupportedOsLabel = "Windows 10 and Windows 11";
    public const string RuntimeLabel = ".NET 10 (self-contained)";
    public const string MinWindowsVersion = "10.0";
#endif

    public const string Publisher = "Skywere Industries";
}
