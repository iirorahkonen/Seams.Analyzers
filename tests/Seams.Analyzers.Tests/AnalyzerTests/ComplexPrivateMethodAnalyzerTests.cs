using System.Threading.Tasks;
using Seams.Analyzers.Analyzers.InheritanceBlockers;
using Seams.Analyzers.Tests.Verifiers;
using Xunit;

namespace Seams.Analyzers.Tests.AnalyzerTests;

public class ComplexPrivateMethodAnalyzerTests
{
    [Fact]
    public async Task ShortPrivateMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class OrderProcessor
            {
                private void ProcessItem()
                {
                    var x = 1;
                    var y = 2;
                }
            }
            """;

        await CSharpAnalyzerVerifier<ComplexPrivateMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PublicMethod_ShouldNotReportDiagnostic()
    {
        // Even if long, public methods are not flagged by this analyzer
        const string source = """
            public class OrderProcessor
            {
                public void ProcessItem()
                {
                    var line1 = 1;
                    var line2 = 2;
                    var line3 = 3;
                    var line4 = 4;
                    var line5 = 5;
                    var line6 = 6;
                    var line7 = 7;
                    var line8 = 8;
                    var line9 = 9;
                    var line10 = 10;
                }
            }
            """;

        await CSharpAnalyzerVerifier<ComplexPrivateMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ExpressionBodyPrivateMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Calculator
            {
                private int Calculate(int x, int y) => x + y;
            }
            """;

        await CSharpAnalyzerVerifier<ComplexPrivateMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodWithNoBody_ShouldNotReportDiagnostic()
    {
        const string source = """
            public abstract class BaseProcessor
            {
                protected abstract void Process();
            }
            """;

        await CSharpAnalyzerVerifier<ComplexPrivateMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodWithDefaultAccessModifier_LongMethod_ShouldReportDiagnostic()
    {
        // Methods without access modifiers are private by default
        // Create a method with 50+ lines
        var lines = new System.Text.StringBuilder();
        for (int i = 1; i <= 55; i++)
        {
            lines.AppendLine($"        var line{i} = {i};");
        }

        var source = $@"public class OrderProcessor
{{
    void {{|#0:ProcessItem|}}()
    {{
{lines}    }}
}}";

        var expected = CSharpAnalyzerVerifier<ComplexPrivateMethodAnalyzer>
            .Diagnostic("SEAM011")
            .WithLocation(0)
            .WithArguments("ProcessItem", 58);

        await CSharpAnalyzerVerifier<ComplexPrivateMethodAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ExplicitPrivateMethod_LongMethod_ShouldReportDiagnostic()
    {
        var lines = new System.Text.StringBuilder();
        for (int i = 1; i <= 55; i++)
        {
            lines.AppendLine($"        var line{i} = {i};");
        }

        var source = $@"public class OrderProcessor
{{
    private void {{|#0:ProcessItem|}}()
    {{
{lines}    }}
}}";

        var expected = CSharpAnalyzerVerifier<ComplexPrivateMethodAnalyzer>
            .Diagnostic("SEAM011")
            .WithLocation(0)
            .WithArguments("ProcessItem", 58);

        await CSharpAnalyzerVerifier<ComplexPrivateMethodAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
