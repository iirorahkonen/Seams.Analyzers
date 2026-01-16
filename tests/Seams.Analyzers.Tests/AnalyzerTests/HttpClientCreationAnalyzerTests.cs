using System.Threading.Tasks;
using Seams.Analyzers.Analyzers.Infrastructure;
using Seams.Analyzers.Tests.Verifiers;
using Xunit;

namespace Seams.Analyzers.Tests.AnalyzerTests;

public class HttpClientCreationAnalyzerTests
{
    [Fact]
    public async Task NewHttpClient_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Net.Http;

            public class ApiClient
            {
                public void SendRequest()
                {
                    var client = {|#0:new HttpClient()|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>
            .Diagnostic("SEAM016")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NewHttpClientInMethod_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Net.Http;
            using System.Threading.Tasks;

            public class ApiClient
            {
                public async Task<string> GetData()
                {
                    using var client = {|#0:new HttpClient()|};
                    return await client.GetStringAsync("https://api.example.com");
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>
            .Diagnostic("SEAM016")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task StaticReadonlyHttpClient_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Net.Http;

            public class ApiClient
            {
                private static readonly HttpClient Client = new HttpClient();
            }
            """;

        await CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task HttpClientInFactoryClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Net.Http;

            public class HttpClientFactory
            {
                public HttpClient Create()
                {
                    return new HttpClient();
                }
            }
            """;

        await CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task HttpClientInCreateClientMethod_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Net.Http;

            public class ApiClientBuilder
            {
                public HttpClient CreateClient()
                {
                    return new HttpClient();
                }
            }
            """;

        await CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InjectedHttpClient_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Net.Http;
            using System.Threading.Tasks;

            public class ApiClient
            {
                private readonly HttpClient _client;

                public ApiClient(HttpClient client)
                {
                    _client = client;
                }

                public async Task<string> GetData()
                {
                    return await _client.GetStringAsync("https://api.example.com");
                }
            }
            """;

        await CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task IHttpClientFactory_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Net.Http;
            using System.Threading.Tasks;

            public interface IHttpClientFactory
            {
                HttpClient CreateClient(string name);
            }

            public class ApiClient
            {
                private readonly IHttpClientFactory _factory;

                public ApiClient(IHttpClientFactory factory)
                {
                    _factory = factory;
                }

                public async Task<string> GetData()
                {
                    using var client = _factory.CreateClient("api");
                    return await client.GetStringAsync("https://api.example.com");
                }
            }
            """;

        await CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ImplicitNewHttpClient_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Net.Http;

            public class ApiClient
            {
                private HttpClient _client = {|#0:new()|};
            }
            """;

        var expected = CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>
            .Diagnostic("SEAM016")
            .WithLocation(0);

        await CSharpAnalyzerVerifier<HttpClientCreationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
