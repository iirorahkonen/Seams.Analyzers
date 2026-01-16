using System.Threading.Tasks;
using TestHarness.Analyzers.Analyzers.InheritanceBlockers;
using TestHarness.Analyzers.Tests.Verifiers;
using Xunit;

namespace TestHarness.Analyzers.Tests.AnalyzerTests;

public class NonVirtualMethodAnalyzerTests
{
    [Fact]
    public async Task PublicNonVirtualMethod_ShouldReportDiagnostic()
    {
        const string source = """
            public class OrderService
            {
                public void {|#0:ProcessOrder|}()
                {
                    // Complex processing logic
                    var x = 1;
                    var y = 2;
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>
            .Diagnostic("SEAM010")
            .WithLocation(0)
            .WithArguments("ProcessOrder");

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task VirtualMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class OrderService
            {
                public virtual void ProcessOrder()
                {
                    // Complex processing logic
                    var x = 1;
                    var y = 2;
                }
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task OverrideMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class BaseService
            {
                public virtual void Process() { }
            }

            public class OrderService : BaseService
            {
                public override void Process()
                {
                    base.Process();
                }
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PrivateMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class OrderService
            {
                private void ProcessOrder()
                {
                    // Complex processing logic
                    var x = 1;
                    var y = 2;
                }
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StaticMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class OrderService
            {
                public static void ProcessOrder()
                {
                    // Complex processing logic
                    var x = 1;
                    var y = 2;
                }
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodInSealedClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            public sealed class OrderService
            {
                public void ProcessOrder()
                {
                    // Complex processing logic
                    var x = 1;
                    var y = 2;
                }
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodInInternalClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            internal class OrderService
            {
                public void ProcessOrder()
                {
                    // Complex processing logic
                    var x = 1;
                    var y = 2;
                }
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InterfaceImplementation_ShouldNotReportDiagnostic()
    {
        const string source = """
            public interface IOrderService
            {
                void ProcessOrder();
            }

            public class OrderService : IOrderService
            {
                public void ProcessOrder()
                {
                    // Implementation
                    var x = 1;
                    var y = 2;
                }
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DisposeMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public class OrderService : IDisposable
            {
                public void Dispose()
                {
                    // Cleanup
                }
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ToStringMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Order
            {
                public int Id { get; set; }

                public new string ToString()
                {
                    return $"Order {Id}";
                }
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ExpressionBodyMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Order
            {
                public int Id { get; set; }

                public string GetId() => Id.ToString();
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SingleReturnStatementMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Order
            {
                public int Id { get; set; }

                public int GetId()
                {
                    return Id;
                }
            }
            """;

        await CSharpAnalyzerVerifier<NonVirtualMethodAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
