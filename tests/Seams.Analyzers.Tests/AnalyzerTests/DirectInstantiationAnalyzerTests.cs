using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Seams.Analyzers.Analyzers.DirectDependencies;
using Seams.Analyzers.Tests.Verifiers;
using Xunit;

namespace Seams.Analyzers.Tests.AnalyzerTests;

public class DirectInstantiationAnalyzerTests
{
    [Fact]
    public async Task DirectInstantiation_HttpClient_HandledByDedicatedAnalyzer()
    {
        // HttpClient is handled by HttpClientCreationAnalyzer (SEAM016), not DirectInstantiationAnalyzer
        const string source = """
            using System.Net.Http;

            public class ApiClient
            {
                public void SendRequest()
                {
                    var client = new HttpClient();
                }
            }
            """;

        // DirectInstantiationAnalyzer should NOT flag this - it's handled elsewhere
        await CSharpAnalyzerVerifier<DirectInstantiationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DirectInstantiation_List_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public class OrderProcessor
            {
                public void Process()
                {
                    var items = new List<string>();
                }
            }
            """;

        await CSharpAnalyzerVerifier<DirectInstantiationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DirectInstantiation_Dictionary_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public class CacheManager
            {
                public void Initialize()
                {
                    var cache = new Dictionary<string, object>();
                }
            }
            """;

        await CSharpAnalyzerVerifier<DirectInstantiationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DirectInstantiation_StringBuilder_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Text;

            public class TextBuilder
            {
                public string Build()
                {
                    var sb = new StringBuilder();
                    return sb.ToString();
                }
            }
            """;

        await CSharpAnalyzerVerifier<DirectInstantiationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DirectInstantiation_LocalClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Order
            {
                public int Id { get; set; }
            }

            public class OrderProcessor
            {
                public void Process()
                {
                    var order = new Order();
                }
            }
            """;

        await CSharpAnalyzerVerifier<DirectInstantiationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DirectInstantiation_Exception_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System;

            public class Validator
            {
                public void Validate(string input)
                {
                    if (string.IsNullOrEmpty(input))
                        throw new ArgumentException("Input required");
                }
            }
            """;

        await CSharpAnalyzerVerifier<DirectInstantiationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DirectInstantiation_InStaticReadonlyField_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Net.Http;

            public class ApiClient
            {
                private static readonly HttpClient Client = new HttpClient();
            }
            """;

        await CSharpAnalyzerVerifier<DirectInstantiationAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
