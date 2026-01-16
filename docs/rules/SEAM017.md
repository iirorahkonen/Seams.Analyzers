# SEAM017: Direct Database Connection Creation

| Property | Value |
|----------|-------|
| **Rule ID** | SEAM017 |
| **Category** | Infrastructure |
| **Severity** | Info |
| **Enabled** | Yes |

## Description

Detects direct instantiation of database connection types like `SqlConnection`, `NpgsqlConnection`, `MySqlConnection`, and other `DbConnection` subclasses that create tight coupling to database infrastructure.

## Why This Is Problematic

Direct database connection creation causes testing challenges:

1. **Requires Real Database**: Unit tests need an actual database, making them slow and fragile
2. **Connection String Management**: Tests need access to valid connection strings
3. **Test Data Setup**: Each test needs to set up and tear down test data
4. **Parallel Test Issues**: Tests may conflict when accessing shared database state
5. **CI/CD Complexity**: Build servers need database access configured
6. **Tight Coupling**: Code is coupled to specific database implementations
7. **Hidden Infrastructure**: Database dependency isn't visible in the class API

## Detected Types

The analyzer detects creation of:

- `SqlConnection` (System.Data.SqlClient, Microsoft.Data.SqlClient)
- `NpgsqlConnection` (PostgreSQL)
- `MySqlConnection` (MySQL)
- `SqliteConnection` (SQLite)
- `OracleConnection` (Oracle)
- Any type inheriting from `DbConnection` or implementing `IDbConnection`

## Examples

### Non-Compliant Code

```csharp
public class OrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Order GetById(int id)
    {
        // Bad: Direct SqlConnection creation
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = new SqlCommand("SELECT * FROM Orders WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = command.ExecuteReader();
        // ...
    }
}
```

```csharp
public class UserService
{
    public User? FindUser(string email)
    {
        // Bad: Creating connection inline
        using var conn = new NpgsqlConnection(
            Environment.GetEnvironmentVariable("DB_CONNECTION"));

        conn.Open();
        // ...
    }
}
```

### Compliant Code

Using Repository Pattern:

```csharp
public interface IOrderRepository
{
    Order? GetById(int id);
    void Save(Order order);
}

public class SqlOrderRepository : IOrderRepository
{
    private readonly IDbConnection _connection;

    public SqlOrderRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public Order? GetById(int id)
    {
        // Connection is injected, can be mocked
        return _connection.QuerySingleOrDefault<Order>(
            "SELECT * FROM Orders WHERE Id = @Id",
            new { Id = id });
    }
}

// DI Registration
services.AddScoped<IDbConnection>(sp =>
{
    var connection = new SqlConnection(connectionString);
    connection.Open();
    return connection;
});
services.AddScoped<IOrderRepository, SqlOrderRepository>();
```

Using DbContext (Entity Framework):

```csharp
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public Order? GetById(int id)
    {
        return _context.Orders.Find(id);
    }
}

// Test with in-memory database
[Fact]
public void GetById_ReturnsOrder()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;

    using var context = new AppDbContext(options);
    context.Orders.Add(new Order { Id = 1, CustomerId = 100 });
    context.SaveChanges();

    var repository = new OrderRepository(context);
    var result = repository.GetById(1);

    Assert.NotNull(result);
    Assert.Equal(100, result.CustomerId);
}
```

Using connection factory:

```csharp
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Order? GetById(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return connection.QuerySingleOrDefault<Order>(
            "SELECT * FROM Orders WHERE Id = @Id",
            new { Id = id });
    }
}
```

## How to Fix

1. **Use Repository Pattern**: Abstract data access behind interfaces
2. **Inject IDbConnection**: Accept connection via constructor
3. **Use DbContext**: Leverage Entity Framework's testable patterns
4. **Create Connection Factory**: Abstract connection creation
5. **Use In-Memory Databases**: SQLite in-memory or EF InMemory for tests
6. **Register in DI**: Set up connection creation in your DI container

### Testing Strategies

```csharp
// Option 1: Mock the repository
var mockRepository = new Mock<IOrderRepository>();
mockRepository.Setup(r => r.GetById(1))
    .Returns(new Order { Id = 1, CustomerId = 100 });

// Option 2: Use SQLite in-memory
var connection = new SqliteConnection("DataSource=:memory:");
connection.Open();
// Set up schema and test data...

// Option 3: Use Testcontainers for integration tests
await using var container = new PostgreSqlBuilder().Build();
await container.StartAsync();
var connectionString = container.GetConnectionString();
```

## When to Suppress

Suppression is appropriate when:

- You're in a **Repository class** that is the designated data access layer
- You're implementing the **connection factory** itself
- You're writing **integration tests** that intentionally test database behavior
- You're in **migration or seed code** that runs at startup

```csharp
#pragma warning disable SEAM017
// This IS the repository that wraps database access
public class SqlOrderRepository : IOrderRepository
{
    public Order? GetById(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        // ...
    }
}
#pragma warning restore SEAM017
```

## Configuration

```ini
# .editorconfig

# Disable the rule entirely
dotnet_diagnostic.SEAM017.severity = none

# Or set to suggestion
dotnet_diagnostic.SEAM017.severity = suggestion
```

## Related Rules

- [SEAM001](SEAM001.md) - Direct Instantiation
- [SEAM015](SEAM015.md) - File System Access
- [SEAM016](SEAM016.md) - HttpClient Creation
- [SEAM018](SEAM018.md) - Process.Start

## References

- [Working Effectively with Legacy Code](https://www.amazon.com/Working-Effectively-Legacy-Michael-Feathers/dp/0131177052) by Michael Feathers
- [Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Testing with InMemory Provider](https://docs.microsoft.com/en-us/ef/core/testing/testing-without-the-database)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
