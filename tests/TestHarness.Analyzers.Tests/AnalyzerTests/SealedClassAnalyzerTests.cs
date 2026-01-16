using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using TestHarness.Analyzers.Analyzers.InheritanceBlockers;
using TestHarness.Analyzers.Tests.Verifiers;
using Xunit;

namespace TestHarness.Analyzers.Tests.AnalyzerTests;

public class SealedClassAnalyzerTests
{
    [Fact]
    public async Task PublicSealedClass_WithInstanceMethods_ShouldReportDiagnostic()
    {
        const string source = """
            public {|#0:sealed|} class UserRepository
            {
                public void GetById(int id) { }
            }
            """;

        var expected = CSharpAnalyzerVerifier<SealedClassAnalyzer>
            .Diagnostic(DiagnosticIds.SealedClass)
            .WithLocation(0)
            .WithArguments("UserRepository");

        await CSharpAnalyzerVerifier<SealedClassAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task InternalSealedClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            internal sealed class UserRepository
            {
                public void GetById(int id) { }
            }
            """;

        await CSharpAnalyzerVerifier<SealedClassAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PrivateSealedClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Outer
            {
                private sealed class Inner
                {
                    public void DoWork() { }
                }
            }
            """;

        await CSharpAnalyzerVerifier<SealedClassAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SealedException_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public sealed class CustomException : Exception
            {
                public void LogError() { }
            }
            """;

        await CSharpAnalyzerVerifier<SealedClassAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SealedAttribute_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public sealed class CustomAttribute : Attribute
            {
                public void Validate() { }
            }
            """;

        await CSharpAnalyzerVerifier<SealedClassAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SealedClassWithOnlyProperties_ShouldNotReportDiagnostic()
    {
        const string source = """
            public sealed class UserDto
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerVerifier<SealedClassAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task UnsealedClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class UserRepository
            {
                public void GetById(int id) { }
            }
            """;

        await CSharpAnalyzerVerifier<SealedClassAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
