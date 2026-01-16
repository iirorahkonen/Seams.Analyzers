using System.Threading.Tasks;
using TestHarness.Analyzers.Analyzers.StaticDependencies;
using TestHarness.Analyzers.Tests.Verifiers;
using Xunit;

namespace TestHarness.Analyzers.Tests.AnalyzerTests;

public class StaticPropertyAccessAnalyzerTests
{
    [Fact]
    public async Task EnvironmentCurrentDirectory_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class DirectoryHelper
            {
                public string GetCurrentDir()
                {
                    return {|#0:Environment.CurrentDirectory|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>
            .Diagnostic("SEAM008")
            .WithLocation(0)
            .WithArguments("Environment", "CurrentDirectory");

        await CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task EnvironmentMachineName_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class MachineInfo
            {
                public string GetMachineName()
                {
                    return {|#0:Environment.MachineName|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>
            .Diagnostic("SEAM008")
            .WithLocation(0)
            .WithArguments("Environment", "MachineName");

        await CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task EnvironmentUserName_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class UserInfo
            {
                public string GetUserName()
                {
                    return {|#0:Environment.UserName|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>
            .Diagnostic("SEAM008")
            .WithLocation(0)
            .WithArguments("Environment", "UserName");

        await CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ThreadCurrentThread_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Threading;

            public class ThreadInfo
            {
                public Thread GetCurrent()
                {
                    return {|#0:Thread.CurrentThread|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>
            .Diagnostic("SEAM008")
            .WithLocation(0)
            .WithArguments("Thread", "CurrentThread");

        await CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task CultureInfoCurrentCulture_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Globalization;

            public class CultureHelper
            {
                public CultureInfo GetCulture()
                {
                    return {|#0:CultureInfo.CurrentCulture|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>
            .Diagnostic("SEAM008")
            .WithLocation(0)
            .WithArguments("CultureInfo", "CurrentCulture");

        await CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task CultureInfoCurrentUICulture_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Globalization;

            public class CultureHelper
            {
                public CultureInfo GetUICulture()
                {
                    return {|#0:CultureInfo.CurrentUICulture|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>
            .Diagnostic("SEAM008")
            .WithLocation(0)
            .WithArguments("CultureInfo", "CurrentUICulture");

        await CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
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

        await CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InstanceProperty_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Order
            {
                public string Name { get; set; } = "";
            }

            public class OrderProcessor
            {
                public string GetName(Order order)
                {
                    return order.Name;
                }
            }
            """;

        await CSharpAnalyzerVerifier<StaticPropertyAccessAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
