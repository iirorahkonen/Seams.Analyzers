using System.Threading.Tasks;
using Seams.Analyzers.Analyzers.GlobalState;
using Seams.Analyzers.Tests.Verifiers;
using Xunit;

namespace Seams.Analyzers.Tests.AnalyzerTests;

public class StaticMutableFieldAnalyzerTests
{
    [Fact]
    public async Task StaticMutableList_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public class Cache
            {
                private static List<string> {|#0:_items|} = new();
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>
            .Diagnostic("SEAM013")
            .WithLocation(0)
            .WithArguments("_items");

        await CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task StaticMutableDictionary_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public class Cache
            {
                private static Dictionary<string, object> {|#0:_cache|} = new();
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>
            .Diagnostic("SEAM013")
            .WithLocation(0)
            .WithArguments("_cache");

        await CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task StaticMutableArray_ShouldReportDiagnostic()
    {
        const string source = """
            public class Cache
            {
                private static int[] {|#0:_values|} = new int[10];
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>
            .Diagnostic("SEAM013")
            .WithLocation(0)
            .WithArguments("_values");

        await CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task StaticReadonlyField_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public class Cache
            {
                private static readonly List<string> _items = new();
            }
            """;

        await CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ConstField_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class Config
            {
                private const int MaxItems = 100;
            }
            """;

        await CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InstanceField_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public class Cache
            {
                private List<string> _items = new();
            }
            """;

        await CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StaticStringField_ShouldNotReportDiagnostic()
    {
        // Strings are immutable
        const string source = """
            public class Config
            {
                private static string _setting = "default";
            }
            """;

        await CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StaticImmutableCollection_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Immutable;

            public class Config
            {
                private static ImmutableList<string> _items = ImmutableList<string>.Empty;
            }
            """;

        await CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task FieldInTestClass_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public class CacheTests
            {
                private static List<string> _items = new();
            }
            """;

        await CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StaticMutableDelegate_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class EventManager
            {
                public static Action {|#0:OnChanged|};
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>
            .Diagnostic("SEAM013")
            .WithLocation(0)
            .WithArguments("OnChanged");

        await CSharpAnalyzerVerifier<StaticMutableFieldAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
