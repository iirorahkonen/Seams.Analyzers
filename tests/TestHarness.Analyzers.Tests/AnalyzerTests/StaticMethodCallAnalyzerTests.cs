using System.Threading.Tasks;
using TestHarness.Analyzers.Analyzers.StaticDependencies;
using TestHarness.Analyzers.Tests.Verifiers;
using Xunit;

namespace TestHarness.Analyzers.Tests.AnalyzerTests;

public class StaticMethodCallAnalyzerTests
{
    [Fact]
    public async Task FileReadAllText_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class FileProcessor
            {
                public string ReadConfig()
                {
                    return {|#0:File.ReadAllText("config.json")|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>
            .Diagnostic("SEAM004")
            .WithLocation(0)
            .WithArguments("File", "ReadAllText");

        await CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ConsoleWriteLine_ShouldReportDiagnostic()
    {
        const string source = """
            using System;

            public class Logger
            {
                public void Log(string message)
                {
                    {|#0:Console.WriteLine(message)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>
            .Diagnostic("SEAM004")
            .WithLocation(0)
            .WithArguments("Console", "WriteLine");

        await CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DirectoryExists_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class DirectoryChecker
            {
                public bool CheckDirectory(string path)
                {
                    return {|#0:Directory.Exists(path)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>
            .Diagnostic("SEAM004")
            .WithLocation(0)
            .WithArguments("Directory", "Exists");

        await CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task PathCombine_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class PathBuilder
            {
                public string BuildPath()
                {
                    return Path.Combine("dir", "file.txt");
                }
            }
            """;

        await CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PathGetFileName_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class PathHelper
            {
                public string GetName(string path)
                {
                    return Path.GetFileName(path);
                }
            }
            """;

        await CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PathGetExtension_ShouldNotReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class PathHelper
            {
                public string GetExt(string path)
                {
                    return Path.GetExtension(path);
                }
            }
            """;

        await CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InstanceMethodCall_ShouldNotReportDiagnostic()
    {
        const string source = """
            public class OrderService
            {
                public void Process() { }
            }

            public class OrderProcessor
            {
                private readonly OrderService _service = new();

                public void Run()
                {
                    _service.Process();
                }
            }
            """;

        await CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ThreadSleep_ShouldReportDiagnostic()
    {
        const string source = """
            using System.Threading;

            public class Waiter
            {
                public void Wait()
                {
                    {|#0:Thread.Sleep(1000)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>
            .Diagnostic("SEAM004")
            .WithLocation(0)
            .WithArguments("Thread", "Sleep");

        await CSharpAnalyzerVerifier<StaticMethodCallAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }
}
