using System.Threading.Tasks;
using TestHarness.Analyzers.Analyzers.DirectDependencies;
using TestHarness.Analyzers.Tests.Verifiers;
using Xunit;

namespace TestHarness.Analyzers.Tests.AnalyzerTests;

public class ConcreteConstructorParameterAnalyzerTests
{
    [Fact]
    public async Task ConcreteServiceParameter_ShouldReportDiagnostic()
    {
        const string source = """
            public interface IOrderService { }

            public class OrderService : IOrderService { }

            public class OrderProcessor
            {
                public OrderProcessor({|#0:OrderService orderService|})
                {
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>
            .Diagnostic("SEAM002")
            .WithLocation(0)
            .WithArguments("orderService", "OrderService");

        await CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task InterfaceParameter_ShouldNotReportDiagnostic()
    {
        const string source = """
            public interface IOrderService { }

            public class OrderProcessor
            {
                public OrderProcessor(IOrderService orderService)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AbstractClassParameter_ShouldNotReportDiagnostic()
    {
        const string source = """
            public abstract class BaseService { }

            public class OrderProcessor
            {
                public OrderProcessor(BaseService service)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ValueTypeParameter_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class OrderProcessor
            {
                public OrderProcessor(int orderId, bool isActive)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StringParameter_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class OrderProcessor
            {
                public OrderProcessor(string connectionString)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task FuncParameter_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public class OrderProcessor
            {
                public OrderProcessor(Func<string> factory)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ConcreteRepositoryParameter_ShouldReportDiagnostic()
    {
        const string source = """
            public interface IRepository { }

            public class OrderRepository : IRepository { }

            public class OrderService
            {
                public OrderService({|#0:OrderRepository repository|})
                {
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>
            .Diagnostic("SEAM002")
            .WithLocation(0)
            .WithArguments("repository", "OrderRepository");

        await CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ConcreteFactoryParameter_ShouldReportDiagnostic()
    {
        const string source = """
            public interface IWidgetFactory { }

            public class WidgetFactory : IWidgetFactory { }

            public class WidgetService
            {
                public WidgetService({|#0:WidgetFactory factory|})
                {
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>
            .Diagnostic("SEAM002")
            .WithLocation(0)
            .WithArguments("factory", "WidgetFactory");

        await CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DataClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class OrderData
            {
                public int Id { get; set; }
            }

            public class OrderProcessor
            {
                public OrderProcessor(OrderData data)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CancellationTokenParameter_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Threading;

            public class OrderProcessor
            {
                public OrderProcessor(CancellationToken token)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<ConcreteConstructorParameterAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
