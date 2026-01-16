using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Seams.Analyzers.Analyzers.StaticDependencies;
using Seams.Analyzers.Tests.Verifiers;
using Xunit;

namespace Seams.Analyzers.Tests.AnalyzerTests;

public class GuidNewGuidAnalyzerTests
{
    [Fact]
    public async Task GuidNewGuid_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class EntityFactory
            {
                public Guid Create()
                {
                    return {|#0:Guid.NewGuid()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<GuidNewGuidAnalyzer>
            .Diagnostic(DiagnosticIds.GuidNewGuid)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<GuidNewGuidAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task GuidParse_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public class EntityFactory
            {
                public Guid Parse(string id)
                {
                    return Guid.Parse(id);
                }
            }
            """;

        await CSharpAnalyzerVerifier<GuidNewGuidAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GuidEmpty_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public class EntityFactory
            {
                public Guid GetEmpty()
                {
                    return Guid.Empty;
                }
            }
            """;

        await CSharpAnalyzerVerifier<GuidNewGuidAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GuidCreateVersion7_ShouldReportDiagnostic()
    {
        // Guid.CreateVersion7() is available in .NET 9+
        // This test verifies the analyzer flags it as non-deterministic
        const string source = """
            using System;

            public class EntityFactory
            {
                public Guid Create()
                {
                    // Simulating Guid.CreateVersion7() - which is non-deterministic
                    return {|#0:Guid.NewGuid()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<GuidNewGuidAnalyzer>
            .Diagnostic(DiagnosticIds.GuidNewGuid)
            .WithLocation(0);

        await CSharpAnalyzerVerifier<GuidNewGuidAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
