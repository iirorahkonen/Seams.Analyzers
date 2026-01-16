#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor

using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Http;

namespace TestHarness.Analyzers.Samples.Patterns;

// SEAM015 - File System Access
// These examples show direct file system access

public class DocumentService
{
    public bool DocumentExists(string path)
    {
        // SEAM015: Direct file system access
        return File.Exists(path);
    }

    public string ReadDocument(string path)
    {
        // SEAM015: Direct file system access
        return File.ReadAllText(path);
    }

    public void SaveDocument(string path, string content)
    {
        // SEAM015: Direct file system access
        File.WriteAllText(path, content);
    }

    public string[] ListDocuments(string directory)
    {
        // SEAM015: Direct file system access
        return Directory.GetFiles(directory);
    }
}

// SEAM016 - HttpClient Creation
// These examples show direct HttpClient creation

public class ApiClient
{
    public async Task<string> GetDataAsync()
    {
        // SEAM016: Direct HttpClient creation should use IHttpClientFactory
        using var client = new HttpClient();
        return await client.GetStringAsync("https://api.example.com/data");
    }

    public async Task PostDataAsync(string data)
    {
        // SEAM016: Direct HttpClient creation
        var client = new HttpClient();
        await client.PostAsync("https://api.example.com/data", new StringContent(data));
    }
}

// SEAM017 - Database Access
// These examples show direct database connection creation

public class DataAccess
{
    private readonly string _connectionString;

    public DataAccess(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void ExecuteCommand(string sql)
    {
        // SEAM017: Direct database connection creation
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public object? ExecuteScalar(string sql)
    {
        // SEAM017: Direct database connection creation
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }
}

// SEAM018 - Process Start
// These examples show direct Process.Start usage

public class ScriptRunner
{
    public void RunScript(string scriptPath)
    {
        // SEAM018: Direct Process.Start creates dependency on external processes
        Process.Start("bash", scriptPath);
    }

    public void OpenUrl(string url)
    {
        // SEAM018: Direct Process.Start
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    public string RunCommandAndGetOutput(string command)
    {
        // SEAM018: Direct Process.Start
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };

        process.Start();
        return process.StandardOutput.ReadToEnd();
    }
}
