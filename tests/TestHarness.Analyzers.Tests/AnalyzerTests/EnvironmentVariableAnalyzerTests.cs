using System.Threading.Tasks;
using TestHarness.Analyzers.Analyzers.StaticDependencies;
using TestHarness.Analyzers.Tests.Verifiers;
using Xunit;

namespace TestHarness.Analyzers.Tests.AnalyzerTests;

public class EnvironmentVariableAnalyzerTests
{
    [Fact]
    public async Task GetEnvironmentVariable_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class ConfigReader
            {
                public string GetSetting()
                {
                    return {|#0:Environment.GetEnvironmentVariable("MY_SETTING")|}!;
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<EnvironmentVariableAnalyzer>
            .Diagnostic("SEAM007")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<EnvironmentVariableAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task GetEnvironmentVariables_ShouldReportDiagnostic()
    {
        const string source = """
            using System;
            using System.Collections;

            public class ConfigReader
            {
                public IDictionary GetAll()
                {
                    return {|#0:Environment.GetEnvironmentVariables()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<EnvironmentVariableAnalyzer>
            .Diagnostic("SEAM007")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<EnvironmentVariableAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task SetEnvironmentVariable_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class ConfigWriter
            {
                public void SetSetting()
                {
                    {|#0:Environment.SetEnvironmentVariable("MY_SETTING", "value")|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<EnvironmentVariableAnalyzer>
            .Diagnostic("SEAM007")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<EnvironmentVariableAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ExpandEnvironmentVariables_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class PathExpander
            {
                public string Expand(string path)
                {
                    return {|#0:Environment.ExpandEnvironmentVariables(path)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<EnvironmentVariableAnalyzer>
            .Diagnostic("SEAM007")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<EnvironmentVariableAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task EnvironmentNewLine_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public class TextFormatter
            {
                public string GetNewLine()
                {
                    return Environment.NewLine;
                }
            }
            """;

        await CSharpAnalyzerVerifier<EnvironmentVariableAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task OtherStaticMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public static class Config
            {
                public static string GetEnvironmentVariable(string name) => name;
            }

            public class ConfigReader
            {
                public string GetSetting()
                {
                    return Config.GetEnvironmentVariable("MY_SETTING");
                }
            }
            """;

        await CSharpAnalyzerVerifier<EnvironmentVariableAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
