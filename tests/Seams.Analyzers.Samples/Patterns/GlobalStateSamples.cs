#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor

namespace Seams.Analyzers.Samples.Patterns;

// SEAM012 - Singleton Pattern
// These examples show singleton pattern that creates global state

// SEAM012: Singleton pattern creates global state that persists across tests
public class ConfigManager
{
    public static ConfigManager Instance { get; } = new();

    private ConfigManager() { }

    private readonly Dictionary<string, string> _settings = new();

    public string GetSetting(string key) => _settings.TryGetValue(key, out var value) ? value : "";
    public void SetSetting(string key, string value) => _settings[key] = value;
}

// Another singleton variant
// SEAM012: Singleton pattern
public class Logger
{
    private static readonly Logger _instance = new();
    public static Logger Instance => _instance;

    private Logger() { }

    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}

// SEAM013 - Static Mutable Field
// These examples show static mutable fields that create shared state

public class CacheManager
{
    // SEAM013: Static mutable field creates shared state
    public static Dictionary<string, object> Cache = new();

    // SEAM013: Static mutable field
    public static List<string> RecentKeys = new();

    public static void Set(string key, object value)
    {
        Cache[key] = value;
        RecentKeys.Add(key);
    }

    public static object? Get(string key)
    {
        return Cache.TryGetValue(key, out var value) ? value : null;
    }
}

// SEAM014 - Ambient Context
// These examples show ambient context patterns like HttpContext.Current
// Note: These would require ASP.NET references to compile fully

public class UserContext
{
    public string? GetCurrentUser()
    {
        // SEAM014: Ambient context creates hidden dependency
        // In a real ASP.NET app: HttpContext.Current?.User?.Identity?.Name
        // This is commented out because it requires ASP.NET Framework reference
        // return HttpContext.Current?.User?.Identity?.Name;
        return null;
    }

    public string? GetCurrentPrincipal()
    {
        // SEAM014: Ambient context via Thread.CurrentPrincipal
        return System.Threading.Thread.CurrentPrincipal?.Identity?.Name;
    }
}
