using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using TestHarness.Analyzers.Analyzers.GlobalState;
using TestHarness.Analyzers.Tests.Verifiers;
using Xunit;

namespace TestHarness.Analyzers.Tests.AnalyzerTests;

public class SingletonPatternAnalyzerTests
{
    [Fact]
    public async Task ClassicSingleton_WithInstanceProperty_ShouldReportDiagnostic()
    {
        const string source = """
            public class {|#0:ConfigManager|}
            {
                public static ConfigManager Instance { get; } = new ConfigManager();
                private ConfigManager() { }

                public string GetValue(string key) => "";
            }
            """;

        var expected = CSharpAnalyzerVerifier<SingletonPatternAnalyzer>
            .Diagnostic(DiagnosticIds.SingletonPattern)
            .WithLocation(0)
            .WithArguments("ConfigManager");

        await CSharpAnalyzerVerifier<SingletonPatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ClassicSingleton_WithInstanceField_ShouldReportDiagnostic()
    {
        const string source = """
            public class {|#0:Logger|}
            {
                private static readonly Logger _instance = new Logger();
                public static Logger Instance => _instance;

                private Logger() { }

                public void Log(string message) { }
            }
            """;

        var expected = CSharpAnalyzerVerifier<SingletonPatternAnalyzer>
            .Diagnostic(DiagnosticIds.SingletonPattern)
            .WithLocation(0)
            .WithArguments("Logger");

        await CSharpAnalyzerVerifier<SingletonPatternAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task RegularClass_WithPublicConstructor_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class ConfigManager
            {
                public ConfigManager() { }

                public string GetValue(string key) => "";
            }
            """;

        await CSharpAnalyzerVerifier<SingletonPatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StaticClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            public static class Utilities
            {
                public static string Format(string input) => input;
            }
            """;

        await CSharpAnalyzerVerifier<SingletonPatternAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ClassWithPrivateConstructor_NoStaticInstance_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Builder
            {
                private Builder() { }

                public static Builder Create() => new Builder();

                public void Build() { }
            }
            """;

        await CSharpAnalyzerVerifier<SingletonPatternAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
