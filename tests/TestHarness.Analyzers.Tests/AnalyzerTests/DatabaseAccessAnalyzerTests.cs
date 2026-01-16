using System.Threading.Tasks;
using TestHarness.Analyzers.Analyzers.Infrastructure;
using TestHarness.Analyzers.Tests.Verifiers;
using Xunit;

namespace TestHarness.Analyzers.Tests.AnalyzerTests;

public class DatabaseAccessAnalyzerTests
{
    [Fact]
    public async Task NewSqlConnection_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Data.SqlClient;

            public class DataService
            {
                public void Execute()
                {
                    using var connection = {|#0:new SqlConnection("connection string")|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<DatabaseAccessAnalyzer>
            .Diagnostic("SEAM017")
            .WithLocation(0)
            .WithArguments("SqlConnection");

        await CSharpAnalyzerVerifier<DatabaseAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task SqlConnectionInRepositoryClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Data.SqlClient;

            public class OrderRepository
            {
                private readonly string _connectionString;

                public OrderRepository(string connectionString)
                {
                    _connectionString = connectionString;
                }

                public void Execute()
                {
                    using var connection = new SqlConnection(_connectionString);
                }
            }
            """;

        await CSharpAnalyzerVerifier<DatabaseAccessAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SqlConnectionInDataAccessClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Data.SqlClient;

            public class OrderDataAccess
            {
                private readonly string _connectionString = "";

                public void Execute()
                {
                    using var connection = new SqlConnection(_connectionString);
                }
            }
            """;

        await CSharpAnalyzerVerifier<DatabaseAccessAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SqlConnectionInDbContextClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Data.SqlClient;

            public class AppDbContext
            {
                private readonly string _connectionString = "";

                public void Execute()
                {
                    using var connection = new SqlConnection(_connectionString);
                }
            }
            """;

        await CSharpAnalyzerVerifier<DatabaseAccessAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SqlConnectionInConnectionFactory_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Data.SqlClient;

            public class SqlConnectionFactory
            {
                private readonly string _connectionString = "";

                public SqlConnection Create()
                {
                    return new SqlConnection(_connectionString);
                }
            }
            """;

        await CSharpAnalyzerVerifier<DatabaseAccessAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InjectedConnection_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Data;

            public interface IConnectionFactory
            {
                IDbConnection Create();
            }

            public class DataService
            {
                private readonly IConnectionFactory _factory;

                public DataService(IConnectionFactory factory)
                {
                    _factory = factory;
                }

                public void Execute()
                {
                    using var connection = _factory.Create();
                }
            }
            """;

        await CSharpAnalyzerVerifier<DatabaseAccessAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ImplicitNewSqlConnection_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Data.SqlClient;

            public class DataService
            {
                private SqlConnection _connection = {|#0:new("connection string")|};
            }
            """;

        var expected = CSharpAnalyzerVerifier<DatabaseAccessAnalyzer>
            .Diagnostic("SEAM017")
            .WithLocation(0)
            .WithArguments("SqlConnection");

        await CSharpAnalyzerVerifier<DatabaseAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
