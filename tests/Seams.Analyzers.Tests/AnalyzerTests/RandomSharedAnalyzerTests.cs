using System.Threading.Tasks;
using Seams.Analyzers.Analyzers.StaticDependencies;
using Seams.Analyzers.Tests.Verifiers;
using Xunit;

namespace Seams.Analyzers.Tests.AnalyzerTests;

public class RandomSharedAnalyzerTests
{
    [Fact]
    public async Task RandomSharedNext_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class RandomService
            {
                public int GetRandomValue()
                {
                    return {|#0:Random.Shared.Next()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<RandomSharedAnalyzer>
            .Diagnostic(DiagnosticIds.RandomShared)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<RandomSharedAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task RandomSharedNextWithMax_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class RandomService
            {
                public int GetRandomValue()
                {
                    return {|#0:Random.Shared.Next(100)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<RandomSharedAnalyzer>
            .Diagnostic(DiagnosticIds.RandomShared)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<RandomSharedAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task RandomSharedNextWithMinMax_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class RandomService
            {
                public int GetRandomValue()
                {
                    return {|#0:Random.Shared.Next(1, 100)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<RandomSharedAnalyzer>
            .Diagnostic(DiagnosticIds.RandomShared)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<RandomSharedAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task RandomSharedNextDouble_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class RandomService
            {
                public double GetRandomValue()
                {
                    return {|#0:Random.Shared.NextDouble()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<RandomSharedAnalyzer>
            .Diagnostic(DiagnosticIds.RandomShared)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<RandomSharedAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task RandomSharedNextInt64_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class RandomService
            {
                public long GetRandomValue()
                {
                    return {|#0:Random.Shared.NextInt64()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<RandomSharedAnalyzer>
            .Diagnostic(DiagnosticIds.RandomShared)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<RandomSharedAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task RandomSharedNextBytes_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class RandomService
            {
                public void FillBuffer(byte[] buffer)
                {
                    {|#0:Random.Shared.NextBytes(buffer)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<RandomSharedAnalyzer>
            .Diagnostic(DiagnosticIds.RandomShared)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<RandomSharedAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task InjectedRandomNext_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public class RandomService
            {
                private readonly Random _random;

                public RandomService(Random random)
                {
                    _random = random;
                }

                public int GetRandomValue()
                {
                    return _random.Next();
                }
            }
            """;

        await CSharpAnalyzerVerifier<RandomSharedAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NewRandomNext_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public class RandomService
            {
                public int GetRandomValue()
                {
                    var random = new Random(42);
                    return random.Next();
                }
            }
            """;

        await CSharpAnalyzerVerifier<RandomSharedAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
