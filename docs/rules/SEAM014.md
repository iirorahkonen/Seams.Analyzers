# SEAM014: Ambient Context Creates Hidden Dependency

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM014 |
| **Category** | GlobalState |
| **Severity** | Warning |
| **Enabled** | Yes |

## Description

Detects usage of ambient context patterns like `HttpContext.Current`, `Thread.CurrentPrincipal`, and `CallContext` that create hidden dependencies difficult to control in tests.

## Why This Is Problematic

Ambient contexts create significant testability challenges:

1. **Hidden Dependencies**: The dependency isn't visible in constructor or method signatures
2. **Test Setup Complexity**: Tests must set up thread-local or async-local state before calling the code
3. **Async/Await Issues**: Many ambient contexts don't flow correctly across async boundaries
4. **Thread Affinity**: Some contexts are tied to specific threads, breaking in parallel tests
5. **Magic Dependencies**: Code appears to work without dependencies but fails without proper context
6. **Framework Coupling**: Code becomes tightly coupled to specific frameworks (ASP.NET, WCF, etc.)

## Detected Patterns

The analyzer detects these ambient context usages:

- `HttpContext.Current`
- `Thread.CurrentThread`, `Thread.CurrentPrincipal`
- `ClaimsPrincipal.Current`
- `SynchronizationContext.Current`
- `CallContext.GetData()`, `CallContext.LogicalGetData()`
- `Transaction.Current`
- `OperationContext.Current`

## Examples

### Non-Compliant Code

```csharp
public class UserService
{
    public string GetCurrentUserName()
    {
        // Bad: Hidden dependency on HttpContext
        var context = HttpContext.Current;
        return context?.User?.Identity?.Name ?? "Anonymous";
    }
}
```

```csharp
public class AuditLogger
{
    public void LogAction(string action)
    {
        // Bad: Hidden dependency on thread principal
        var principal = Thread.CurrentPrincipal;
        var userId = principal?.Identity?.Name ?? "Unknown";

        Console.WriteLine($"[{DateTime.Now}] {userId}: {action}");
    }
}
```

```csharp
public class TransactionManager
{
    public void ExecuteInTransaction(Action action)
    {
        // Bad: Relying on ambient transaction
        if (Transaction.Current == null)
        {
            throw new InvalidOperationException("No ambient transaction");
        }

        action();
    }
}
```

### Compliant Code

```csharp
public class UserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Good: Explicit dependency injection
    public UserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentUserName()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.User?.Identity?.Name ?? "Anonymous";
    }
}
```

```csharp
public interface IUserContext
{
    string? UserId { get; }
    string? UserName { get; }
}

public class AuditLogger : IAuditLogger
{
    private readonly IUserContext _userContext;
    private readonly ILogger<AuditLogger> _logger;

    // Good: Dependencies injected explicitly
    public AuditLogger(IUserContext userContext, ILogger<AuditLogger> logger)
    {
        _userContext = userContext;
        _logger = logger;
    }

    public void LogAction(string action)
    {
        var userId = _userContext.UserId ?? "Unknown";
        _logger.LogInformation("[{Time}] {UserId}: {Action}", DateTime.UtcNow, userId, action);
    }
}
```

```csharp
public class TransactionManager : ITransactionManager
{
    private readonly IDbConnection _connection;

    public TransactionManager(IDbConnection connection)
    {
        _connection = connection;
    }

    // Good: Transaction explicitly passed or created
    public void ExecuteInTransaction(Action<IDbTransaction> action)
    {
        using var transaction = _connection.BeginTransaction();
        try
        {
            action(transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

## How to Fix

1. **Identify Ambient Context Usage**: Find all references to `*.Current` properties or `CallContext` methods
2. **Create Abstraction**: Define an interface that represents the needed context
3. **Implement Adapter**: Create an implementation that wraps the ambient context
4. **Register in DI**: Add the adapter to your DI container
5. **Inject Dependencies**: Replace ambient context access with injected dependencies
6. **Create Test Doubles**: Implement the interface with controllable test values

### ASP.NET Core Migration

```csharp
// Old: Using HttpContext.Current
public string GetUserId() => HttpContext.Current?.User?.Identity?.Name;

// New: Using IHttpContextAccessor
public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public UserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? UserId => _accessor.HttpContext?.User?.Identity?.Name;
}

// In Startup/Program.cs
services.AddHttpContextAccessor();
services.AddScoped<IUserContext, UserContext>();
```

## When to Suppress

Suppression is appropriate when:

- You're in **framework infrastructure code** that must use ambient context
- You're working with **legacy ASP.NET WebForms** or similar frameworks
- The code is in the **composition root** or startup configuration
- You're implementing the **adapter** that wraps the ambient context for DI

```csharp
#pragma warning disable SEAM014
// This is the adapter that wraps the ambient context
public class HttpContextUserContext : IUserContext
{
    public string? UserId => HttpContext.Current?.User?.Identity?.Name;
}
#pragma warning restore SEAM014
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM014.severity = none

# Or set to suggestion instead of warning
dotnet_diagnostic.SEAM014.severity = suggestion
```

## Related Rules

- [SEAM012](SEAM012.md) - Singleton Pattern (another form of global state)
- [SEAM013](SEAM013.md) - Static Mutable Fields (related shared state)
- [SEAM003](SEAM003.md) - Service Locator (similar hidden dependency)
- [SEAM008](SEAM008.md) - Static Property Access (similar pattern)

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Ambient Context is an Anti-Pattern](https://freecontent.manning.com/the-ambient-context-anti-pattern/) by Steven van Deursen
- [IHttpContextAccessor documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-context)
