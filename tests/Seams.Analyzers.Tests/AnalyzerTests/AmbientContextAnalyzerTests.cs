using System.Threading.Tasks;
using Seams.Analyzers.Analyzers.GlobalState;
using Seams.Analyzers.Tests.Verifiers;
using Xunit;

namespace Seams.Analyzers.Tests.AnalyzerTests;

public class AmbientContextAnalyzerTests
{
    [Fact]
    public async Task ThreadCurrentThread_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Threading;

            public class ThreadService
            {
                public Thread GetThread()
                {
                    return {|#0:Thread.CurrentThread|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AmbientContextAnalyzer>
            .Diagnostic("SEAM014")
            .WithLocation(0)
            .WithArguments("Thread.CurrentThread");

        await CSharpAnalyzerVerifier<AmbientContextAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ThreadCurrentPrincipal_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Security.Principal;
            using System.Threading;

            public class SecurityService
            {
                public IPrincipal GetPrincipal()
                {
                    return {|#0:Thread.CurrentPrincipal|}!;
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AmbientContextAnalyzer>
            .Diagnostic("SEAM014")
            .WithLocation(0)
            .WithArguments("Thread.CurrentPrincipal");

        await CSharpAnalyzerVerifier<AmbientContextAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ClaimsPrincipalCurrent_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Security.Claims;

            public class SecurityService
            {
                public ClaimsPrincipal GetPrincipal()
                {
                    return {|#0:ClaimsPrincipal.Current|}!;
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AmbientContextAnalyzer>
            .Diagnostic("SEAM014")
            .WithLocation(0)
            .WithArguments("ClaimsPrincipal.Current");

        await CSharpAnalyzerVerifier<AmbientContextAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task SynchronizationContextCurrent_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Threading;

            public class ContextService
            {
                public SynchronizationContext GetContext()
                {
                    return {|#0:SynchronizationContext.Current|}!;
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<AmbientContextAnalyzer>
            .Diagnostic("SEAM014")
            .WithLocation(0)
            .WithArguments("SynchronizationContext.Current");

        await CSharpAnalyzerVerifier<AmbientContextAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task InjectedDependency_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Threading;

            public interface IThreadProvider
            {
                Thread GetCurrentThread();
            }

            public class ThreadService
            {
                private readonly IThreadProvider _provider;

                public ThreadService(IThreadProvider provider)
                {
                    _provider = provider;
                }

                public Thread GetThread()
                {
                    return _provider.GetCurrentThread();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AmbientContextAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RegularStaticProperty_ShouldNotReportDiagnostic()
    {
        const string source = """
            public static class Config
            {
                public static string Current { get; set; } = "";
            }

            public class ConfigReader
            {
                public string GetConfig()
                {
                    return Config.Current;
                }
            }
            """;

        await CSharpAnalyzerVerifier<AmbientContextAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InstancePropertyNamedCurrent_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class StateManager
            {
                public object Current { get; set; } = new();
            }

            public class StateReader
            {
                private readonly StateManager _manager = new();

                public object GetCurrent()
                {
                    return _manager.Current;
                }
            }
            """;

        await CSharpAnalyzerVerifier<AmbientContextAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
