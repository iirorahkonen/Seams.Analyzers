using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Seams.Analyzers.Analyzers.StaticDependencies;
using Seams.Analyzers.Tests.Verifiers;
using Xunit;

namespace Seams.Analyzers.Tests.AnalyzerTests;

public class DateTimeNowAnalyzerTests
{
    [Fact]
    public async Task DateTimeNow_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class AuditLogger
            {
                public void Log(string message)
                {
                    var timestamp = {|#0:DateTime.Now|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<DateTimeNowAnalyzer>
            .Diagnostic(DiagnosticIds.DateTimeNow)
            .WithLocation(0)
            .WithArguments("DateTime.Now");

        await CSharpAnalyzerVerifier<DateTimeNowAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DateTimeUtcNow_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class AuditLogger
            {
                public void Log(string message)
                {
                    var timestamp = {|#0:DateTime.UtcNow|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<DateTimeNowAnalyzer>
            .Diagnostic(DiagnosticIds.DateTimeNow)
            .WithLocation(0)
            .WithArguments("DateTime.UtcNow");

        await CSharpAnalyzerVerifier<DateTimeNowAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DateTimeToday_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class AuditLogger
            {
                public DateTime GetToday()
                {
                    return {|#0:DateTime.Today|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<DateTimeNowAnalyzer>
            .Diagnostic(DiagnosticIds.DateTimeNow)
            .WithLocation(0)
            .WithArguments("DateTime.Today");

        await CSharpAnalyzerVerifier<DateTimeNowAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DateTimeOffsetNow_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class AuditLogger
            {
                public void Log(string message)
                {
                    var timestamp = {|#0:DateTimeOffset.Now|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<DateTimeNowAnalyzer>
            .Diagnostic(DiagnosticIds.DateTimeNow)
            .WithLocation(0)
            .WithArguments("DateTimeOffset.Now");

        await CSharpAnalyzerVerifier<DateTimeNowAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DateTimeMinValue_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public class AuditLogger
            {
                public DateTime GetDefault()
                {
                    return DateTime.MinValue;
                }
            }
            """;

        await CSharpAnalyzerVerifier<DateTimeNowAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DateTimeParse_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public class AuditLogger
            {
                public DateTime Parse(string date)
                {
                    return DateTime.Parse(date);
                }
            }
            """;

        await CSharpAnalyzerVerifier<DateTimeNowAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
