#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor

namespace TestHarness.Analyzers.Samples.Patterns;

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
}

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
