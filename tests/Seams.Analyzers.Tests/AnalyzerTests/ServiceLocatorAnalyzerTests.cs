using System.Threading.Tasks;
using Seams.Analyzers.Analyzers.DirectDependencies;
using Seams.Analyzers.Tests.Verifiers;
using Xunit;

namespace Seams.Analyzers.Tests.AnalyzerTests;

public class ServiceLocatorAnalyzerTests
{
    [Fact]
    public async Task ServiceLocatorResolve_ShouldReportDiagnostic()
    {
        const string source = """
            public static class ServiceLocator
            {
                public static T Resolve<T>() => default!;
            }

            public class OrderService
            {
                public void Process()
                {
                    var repo = {|#0:ServiceLocator.Resolve<object>()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ServiceLocatorAnalyzer>
            .Diagnostic("SEAM003")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<ServiceLocatorAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DependencyResolverGetService_ShouldReportDiagnostic()
    {
        const string source = """
            public static class DependencyResolver
            {
                public static T GetService<T>() => default!;
            }

            public class OrderService
            {
                public void Process()
                {
                    var repo = {|#0:DependencyResolver.GetService<object>()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<ServiceLocatorAnalyzer>
            .Diagnostic("SEAM003")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<ServiceLocatorAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ConstructorInjection_ShouldNotReportDiagnostic()
    {
        const string source = """
            public interface IOrderRepository { }

            public class OrderService
            {
                private readonly IOrderRepository _repository;

                public OrderService(IOrderRepository repository)
                {
                    _repository = repository;
                }
            }
            """;

        await CSharpAnalyzerVerifier<ServiceLocatorAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GetServiceInStartupClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public interface IServiceProvider
            {
                object GetService(Type type);
            }

            public class Startup
            {
                private readonly IServiceProvider _provider = null!;

                public void Configure()
                {
                    var service = _provider.GetService(typeof(object));
                }
            }
            """;

        await CSharpAnalyzerVerifier<ServiceLocatorAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GetServiceInConfigureServicesMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public interface IServiceProvider
            {
                object GetService(Type type);
            }

            public class ServiceConfig
            {
                private readonly IServiceProvider _provider = null!;

                public void ConfigureServices()
                {
                    var service = _provider.GetService(typeof(object));
                }
            }
            """;

        await CSharpAnalyzerVerifier<ServiceLocatorAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GetServiceInFactoryLambda_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public interface IServiceProvider
            {
                T GetService<T>();
            }

            public interface IServiceCollection
            {
                void AddSingleton<T>(Func<IServiceProvider, T> factory);
            }

            public class ServiceConfig
            {
                public void Configure(IServiceCollection services)
                {
                    services.AddSingleton<object>(sp => sp.GetService<object>());
                }
            }
            """;

        await CSharpAnalyzerVerifier<ServiceLocatorAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NormalMethodCall_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class OrderService
            {
                public object GetService() => new object();

                public void Process()
                {
                    var result = GetService();
                }
            }
            """;

        await CSharpAnalyzerVerifier<ServiceLocatorAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
