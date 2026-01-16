#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor

namespace Seams.Analyzers.Samples.Patterns;

// SEAM004 - Static Method Calls
// These examples show calls to static methods that create untestable dependencies

public class FileProcessor
{
    public string ReadConfig()
    {
        // SEAM004: Direct file system access via static method
        return File.ReadAllText("config.json");
    }

    public void WriteLog(string message)
    {
        // SEAM004: Direct console output
        Console.WriteLine(message);
    }
}

// SEAM005 - DateTime.Now
// These examples show usage of DateTime.Now/UtcNow that creates non-deterministic behavior

public class AuditLogger
{
    public void Log(string message)
    {
        // SEAM005: DateTime.Now creates non-deterministic dependency
        var timestamp = DateTime.Now;
        Console.WriteLine($"[{timestamp}] {message}");
    }

    public void LogUtc(string message)
    {
        // SEAM005: DateTime.UtcNow creates non-deterministic dependency
        var utc = DateTime.UtcNow;
        Console.WriteLine($"[{utc}] {message}");
    }

#if NET8_0_OR_GREATER
    public void LogWithTimeProvider(string message)
    {
        // SEAM005: TimeProvider.System.GetUtcNow() also creates non-deterministic dependency
        // Note: TimeProvider was added in .NET 8 - prefer injecting TimeProvider instead
        var utc = TimeProvider.System.GetUtcNow();
        Console.WriteLine($"[{utc}] {message}");
    }

    public void LogWithLocalTime(string message)
    {
        // SEAM005: TimeProvider.System.GetLocalNow() also creates non-deterministic dependency
        var local = TimeProvider.System.GetLocalNow();
        Console.WriteLine($"[{local}] {message}");
    }
#endif
}

// SEAM006 - Guid.NewGuid
// These examples show usage of Guid.NewGuid() that creates non-deterministic behavior

public class Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

public class EntityFactory
{
    public Entity Create(string name)
    {
        // SEAM006: Guid.NewGuid creates non-deterministic dependency
        return new Entity { Id = Guid.NewGuid(), Name = name };
    }

#if NET9_0_OR_GREATER
    public Entity CreateWithVersion7(string name)
    {
        // SEAM006: Guid.CreateVersion7 also creates non-deterministic dependency
        // Note: Available in .NET 9+ only - demonstrates framework-specific patterns
        return new Entity { Id = Guid.CreateVersion7(), Name = name };
    }

    public Entity CreateWithVersion7Timestamp(string name, DateTimeOffset timestamp)
    {
        // SEAM006: Guid.CreateVersion7 with timestamp - still non-deterministic in production
        return new Entity { Id = Guid.CreateVersion7(timestamp), Name = name };
    }
#endif
}

// SEAM019 - Random.Shared
// These examples show usage of Random.Shared that creates non-deterministic behavior

#if NET6_0_OR_GREATER
public class RandomService
{
    public int GetRandomNumber()
    {
        // SEAM019: Random.Shared creates non-deterministic dependency
        // Note: Random.Shared was added in .NET 6 - prefer injecting Random instead
        return Random.Shared.Next();
    }

    public int GetRandomNumberInRange(int min, int max)
    {
        // SEAM019: Random.Shared.Next with range also creates non-deterministic dependency
        return Random.Shared.Next(min, max);
    }

    public double GetRandomDouble()
    {
        // SEAM019: Random.Shared.NextDouble creates non-deterministic dependency
        return Random.Shared.NextDouble();
    }
}
#endif

// SEAM007 - Environment Variables
// These examples show direct access to environment variables

public class ConfigReader
{
    public string GetConnectionString()
    {
        // SEAM007: Direct environment variable access
        return Environment.GetEnvironmentVariable("DB_CONNECTION") ?? "";
    }

    public string GetApiKey()
    {
        // SEAM007: Direct environment variable access
        return Environment.GetEnvironmentVariable("API_KEY") ?? "";
    }
}

// SEAM008 - Static Property Access
// These examples show access to static properties that create untestable dependencies

public class LegacyConfigReader
{
    public string GetSetting(string key)
    {
        // SEAM008: Static property access to ConfigurationManager would trigger this
        // Note: Requires reference to System.Configuration
        return "";
    }

    public string GetMachineName()
    {
        // SEAM008: Static property access to Environment
        return Environment.MachineName;
    }
}
