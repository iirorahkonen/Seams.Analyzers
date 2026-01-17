using System.Threading.Tasks;
using Seams.Analyzers.Analyzers.Complexity;
using Seams.Analyzers.Tests.Verifiers;
using Xunit;

namespace Seams.Analyzers.Tests.AnalyzerTests;

public class HighCyclomaticComplexityAnalyzerTests
{
    [Fact]
    public async Task SimpleMethod_BelowThreshold_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Calculator
            {
                public int Add(int a, int b)
                {
                    return a + b;
                }
            }
            """;

        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodWithIfStatements_BelowThreshold_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Validator
            {
                public bool Validate(int value)
                {
                    if (value < 0) return false;
                    if (value > 100) return false;
                    return true;
                }
            }
            """;

        // Complexity: 1 (base) + 2 (if statements) = 3
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodAtThreshold_ShouldNotReportDiagnostic()
    {
        // Create a method with exactly 25 complexity (threshold)
        // 1 base + 24 if statements = 25
        var ifStatements = new System.Text.StringBuilder();
        for (int i = 1; i <= 24; i++)
        {
            ifStatements.AppendLine($"        if (value == {i}) return {i};");
        }

        var source = $@"public class Processor
{{
    public int Process(int value)
    {{
{ifStatements}        return 0;
    }}
}}";

        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodAboveThreshold_ShouldReportDiagnostic()
    {
        // Create a method with 26 complexity (above threshold of 25)
        // 1 base + 25 if statements = 26
        var ifStatements = new System.Text.StringBuilder();
        for (int i = 1; i <= 25; i++)
        {
            ifStatements.AppendLine($"        if (value == {i}) return {i};");
        }

        var source = $@"public class Processor
{{
    public int {{|#0:Process|}}(int value)
    {{
{ifStatements}        return 0;
    }}
}}";

        var expected = CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>
            .Diagnostic("SEAM020")
            .WithLocation(0)
            .WithArguments("Process", 26, 25);

        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task SwitchExpressionArms_ShouldCountEachArm()
    {
        const string source = """
            public class Processor
            {
                public string GetDayName(int day)
                {
                    return day switch
                    {
                        1 => "Monday",
                        2 => "Tuesday",
                        3 => "Wednesday",
                        4 => "Thursday",
                        5 => "Friday",
                        6 => "Saturday",
                        7 => "Sunday",
                        _ => "Unknown"
                    };
                }
            }
            """;

        // Complexity: 1 (base) + 7 (switch arms, excluding discard _) = 8
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task LogicalOperators_ShouldCountEachOperator()
    {
        const string source = """
            public class Validator
            {
                public bool IsValid(int a, int b, int c, int d)
                {
                    return a > 0 && b > 0 && c > 0 || d > 0;
                }
            }
            """;

        // Complexity: 1 (base) + 2 (&& operators) + 1 (|| operator) = 4
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NullCoalescing_ShouldCountOperator()
    {
        const string source = """
            public class Processor
            {
                public string GetValue(string? input)
                {
                    return input ?? "default";
                }
            }
            """;

        // Complexity: 1 (base) + 1 (?? operator) = 2
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TernaryOperator_ShouldCountOperator()
    {
        const string source = """
            public class Processor
            {
                public int GetMax(int a, int b)
                {
                    return a > b ? a : b;
                }
            }
            """;

        // Complexity: 1 (base) + 1 (?: operator) = 2
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CatchBlocks_ShouldCountEachBlock()
    {
        const string source = """
            using System;

            public class Processor
            {
                public void Process()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (InvalidOperationException)
                    {
                        HandleInvalidOperation();
                    }
                    catch (ArgumentException)
                    {
                        HandleArgument();
                    }
                    catch (Exception)
                    {
                        HandleGeneral();
                    }
                }

                private void DoSomething() { }
                private void HandleInvalidOperation() { }
                private void HandleArgument() { }
                private void HandleGeneral() { }
            }
            """;

        // Complexity: 1 (base) + 3 (catch blocks) = 4
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task LambdasNotCounted_InContainingMethod()
    {
        const string source = """
            using System;
            using System.Linq;
            using System.Collections.Generic;

            public class Processor
            {
                public void Process(List<int> items)
                {
                    // Lambda with complexity should not be counted in containing method
                    var filtered = items.Where(x => x > 0 && x < 100);
                }
            }
            """;

        // Complexity of Process: 1 (base) = 1
        // Lambda is analyzed separately, not counted here
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task LocalFunctions_AnalyzedSeparately()
    {
        const string source = """
            public class Processor
            {
                public int Process(int value)
                {
                    return LocalFunction(value);

                    int LocalFunction(int v)
                    {
                        if (v < 0) return 0;
                        return v;
                    }
                }
            }
            """;

        // Process complexity: 1 (base) = 1
        // LocalFunction complexity: 1 (base) + 1 (if) = 2
        // Both are below threshold
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AbstractMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public abstract class Processor
            {
                public abstract void Process(int value);
            }
            """;

        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InterfaceMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public interface IProcessor
            {
                void Process(int value);
            }
            """;

        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ForLoop_ShouldCountAsDecisionPoint()
    {
        const string source = """
            public class Processor
            {
                public int Sum(int count)
                {
                    int sum = 0;
                    for (int i = 0; i < count; i++)
                    {
                        sum += i;
                    }
                    return sum;
                }
            }
            """;

        // Complexity: 1 (base) + 1 (for) = 2
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ForEachLoop_ShouldCountAsDecisionPoint()
    {
        const string source = """
            using System.Collections.Generic;

            public class Processor
            {
                public int Sum(List<int> items)
                {
                    int sum = 0;
                    foreach (var item in items)
                    {
                        sum += item;
                    }
                    return sum;
                }
            }
            """;

        // Complexity: 1 (base) + 1 (foreach) = 2
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task WhileLoop_ShouldCountAsDecisionPoint()
    {
        const string source = """
            public class Processor
            {
                public int Countdown(int value)
                {
                    while (value > 0)
                    {
                        value--;
                    }
                    return value;
                }
            }
            """;

        // Complexity: 1 (base) + 1 (while) = 2
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DoWhileLoop_ShouldCountAsDecisionPoint()
    {
        const string source = """
            public class Processor
            {
                public int Countdown(int value)
                {
                    do
                    {
                        value--;
                    } while (value > 0);
                    return value;
                }
            }
            """;

        // Complexity: 1 (base) + 1 (do-while) = 2
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SwitchStatementCases_ShouldCountEachCase()
    {
        const string source = """
            public class Processor
            {
                public string GetDayName(int day)
                {
                    switch (day)
                    {
                        case 1:
                            return "Monday";
                        case 2:
                            return "Tuesday";
                        case 3:
                            return "Wednesday";
                        default:
                            return "Unknown";
                    }
                }
            }
            """;

        // Complexity: 1 (base) + 3 (case labels, default not counted as it's not CaseSwitchLabel) = 4
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PatternMatchingCase_ShouldCountEachPattern()
    {
        const string source = """
            public class Processor
            {
                public string Describe(object obj)
                {
                    switch (obj)
                    {
                        case int i:
                            return $"Integer: {i}";
                        case string s:
                            return $"String: {s}";
                        case null:
                            return "Null";
                        default:
                            return "Unknown";
                    }
                }
            }
            """;

        // Complexity: 1 (base) + 3 (case pattern labels) = 4
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DiscardPattern_ShouldNotBeCounted()
    {
        const string source = """
            public class Processor
            {
                public string Describe(int value)
                {
                    return value switch
                    {
                        1 => "One",
                        2 => "Two",
                        _ => "Other"
                    };
                }
            }
            """;

        // Complexity: 1 (base) + 2 (non-discard arms) = 3
        // The _ discard is not counted
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ExpressionBodiedMethod_ShouldAnalyze()
    {
        const string source = """
            public class Processor
            {
                public int GetMax(int a, int b) => a > b ? a : b;
            }
            """;

        // Complexity: 1 (base) + 1 (ternary) = 2
        await CSharpAnalyzerVerifier<HighCyclomaticComplexityAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
