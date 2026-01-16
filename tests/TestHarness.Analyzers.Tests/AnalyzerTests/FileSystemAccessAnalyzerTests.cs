using System.Threading.Tasks;
using TestHarness.Analyzers.Analyzers.Infrastructure;
using TestHarness.Analyzers.Tests.Verifiers;
using Xunit;

namespace TestHarness.Analyzers.Tests.AnalyzerTests;

public class FileSystemAccessAnalyzerTests
{
    [Fact]
    public async Task FileReadAllText_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class FileProcessor
            {
                public string ReadFile(string path)
                {
                    return {|#0:File.ReadAllText(path)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>
            .Diagnostic("SEAM015")
            .WithLocation(0)
            .WithArguments("File.ReadAllText");

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task FileWriteAllText_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class FileProcessor
            {
                public void WriteFile(string path, string content)
                {
                    {|#0:File.WriteAllText(path, content)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>
            .Diagnostic("SEAM015")
            .WithLocation(0)
            .WithArguments("File.WriteAllText");

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task FileExists_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class FileChecker
            {
                public bool CheckFile(string path)
                {
                    return {|#0:File.Exists(path)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>
            .Diagnostic("SEAM015")
            .WithLocation(0)
            .WithArguments("File.Exists");

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DirectoryGetFiles_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class DirectoryScanner
            {
                public string[] GetFiles(string path)
                {
                    return {|#0:Directory.GetFiles(path)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>
            .Diagnostic("SEAM015")
            .WithLocation(0)
            .WithArguments("Directory.GetFiles");

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DirectoryCreateDirectory_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class DirectoryCreator
            {
                public void Create(string path)
                {
                    {|#0:Directory.CreateDirectory(path)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>
            .Diagnostic("SEAM015")
            .WithLocation(0)
            .WithArguments("Directory.CreateDirectory");

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NewFileStream_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class FileProcessor
            {
                public FileStream OpenFile(string path)
                {
                    return {|#0:new FileStream(path, FileMode.Open)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>
            .Diagnostic("SEAM015")
            .WithLocation(0)
            .WithArguments("new FileStream");

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NewStreamReader_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class FileReader
            {
                public StreamReader OpenReader(string path)
                {
                    return {|#0:new StreamReader(path)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>
            .Diagnostic("SEAM015")
            .WithLocation(0)
            .WithArguments("new StreamReader");

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NewStreamWriter_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class FileWriter
            {
                public StreamWriter OpenWriter(string path)
                {
                    return {|#0:new StreamWriter(path)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>
            .Diagnostic("SEAM015")
            .WithLocation(0)
            .WithArguments("new StreamWriter");

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NewFileInfo_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class FileInfoReader
            {
                public FileInfo GetInfo(string path)
                {
                    return {|#0:new FileInfo(path)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>
            .Diagnostic("SEAM015")
            .WithLocation(0)
            .WithArguments("new FileInfo");

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NewDirectoryInfo_ShouldReportDiagnostic()
    {
        const string source = """
            using System.IO;

            public class DirectoryInfoReader
            {
                public DirectoryInfo GetInfo(string path)
                {
                    return {|#0:new DirectoryInfo(path)|};
                }
            }
            """;

        var expected = CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>
            .Diagnostic("SEAM015")
            .WithLocation(0)
            .WithArguments("new DirectoryInfo");

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task PathCombine_ShouldNotReportDiagnostic()
    {
        // Path.Combine is a pure function and doesn't access the file system
        const string source = """
            using System.IO;

            public class PathHelper
            {
                public string CombinePaths(string dir, string file)
                {
                    return Path.Combine(dir, file);
                }
            }
            """;

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InjectedFileSystem_ShouldNotReportDiagnostic()
    {
        const string source = """
            public interface IFileSystem
            {
                string ReadAllText(string path);
            }

            public class FileProcessor
            {
                private readonly IFileSystem _fileSystem;

                public FileProcessor(IFileSystem fileSystem)
                {
                    _fileSystem = fileSystem;
                }

                public string ReadFile(string path)
                {
                    return _fileSystem.ReadAllText(path);
                }
            }
            """;

        await CSharpAnalyzerVerifier<FileSystemAccessAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
