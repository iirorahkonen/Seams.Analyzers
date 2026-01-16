using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers;

/// <summary>
/// Provides access to .editorconfig options for analyzer configuration.
/// </summary>
public static class AnalyzerConfigOptions
{
    private const string Prefix = "dotnet_code_quality.";

    /// <summary>
    /// Gets excluded types for a specific analyzer from .editorconfig.
    /// Example: dotnet_code_quality.SEAM001.excluded_types = T:MyNamespace.AllowedFactory
    /// </summary>
    public static ImmutableHashSet<string> GetExcludedTypes(
        AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string diagnosticId)
    {
        var key = $"{Prefix}{diagnosticId}.excluded_types";
        return GetSymbolList(options, syntaxTree, key);
    }

    /// <summary>
    /// Gets excluded methods for a specific analyzer from .editorconfig.
    /// Example: dotnet_code_quality.SEAM004.excluded_methods = M:System.Console.WriteLine
    /// </summary>
    public static ImmutableHashSet<string> GetExcludedMethods(
        AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string diagnosticId)
    {
        var key = $"{Prefix}{diagnosticId}.excluded_methods";
        return GetSymbolList(options, syntaxTree, key);
    }

    /// <summary>
    /// Gets excluded namespaces for a specific analyzer from .editorconfig.
    /// Example: dotnet_code_quality.SEAM009.excluded_namespaces = MyNamespace.Internal
    /// </summary>
    public static ImmutableHashSet<string> GetExcludedNamespaces(
        AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string diagnosticId)
    {
        var key = $"{Prefix}{diagnosticId}.excluded_namespaces";
        return GetSymbolList(options, syntaxTree, key);
    }

    /// <summary>
    /// Gets the complexity threshold for complex private method detection.
    /// Example: dotnet_code_quality.SEAM011.complexity_threshold = 50
    /// </summary>
    public static int GetComplexityThreshold(
        AnalyzerOptions options,
        SyntaxTree syntaxTree,
        int defaultValue = 50)
    {
        var key = $"{Prefix}{DiagnosticIds.ComplexPrivateMethod}.complexity_threshold";
        var optionsProvider = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);

        if (optionsProvider.TryGetValue(key, out var value) &&
            int.TryParse(value, out var threshold))
        {
            return threshold;
        }

        return defaultValue;
    }

    /// <summary>
    /// Checks if a type should be excluded from analysis.
    /// </summary>
    public static bool IsTypeExcluded(
        INamedTypeSymbol typeSymbol,
        ImmutableHashSet<string> excludedTypes)
    {
        if (excludedTypes.IsEmpty)
            return false;

        var fullName = $"T:{typeSymbol.ToDisplayString()}";
        return excludedTypes.Contains(fullName);
    }

    /// <summary>
    /// Checks if a method should be excluded from analysis.
    /// </summary>
    public static bool IsMethodExcluded(
        IMethodSymbol methodSymbol,
        ImmutableHashSet<string> excludedMethods)
    {
        if (excludedMethods.IsEmpty)
            return false;

        var fullName = $"M:{methodSymbol.ContainingType.ToDisplayString()}.{methodSymbol.Name}";
        return excludedMethods.Contains(fullName);
    }

    /// <summary>
    /// Checks if a namespace should be excluded from analysis.
    /// </summary>
    public static bool IsNamespaceExcluded(
        INamespaceSymbol namespaceSymbol,
        ImmutableHashSet<string> excludedNamespaces)
    {
        if (excludedNamespaces.IsEmpty)
            return false;

        var fullName = namespaceSymbol.ToDisplayString();
        return excludedNamespaces.Contains(fullName);
    }

    /// <summary>
    /// Checks if a type is in an excluded namespace.
    /// </summary>
    public static bool IsInExcludedNamespace(
        INamedTypeSymbol typeSymbol,
        ImmutableHashSet<string> excludedNamespaces)
    {
        if (excludedNamespaces.IsEmpty)
            return false;

        var ns = typeSymbol.ContainingNamespace;
        while (ns != null && !ns.IsGlobalNamespace)
        {
            if (excludedNamespaces.Contains(ns.ToDisplayString()))
                return true;
            ns = ns.ContainingNamespace;
        }

        return false;
    }

    private static ImmutableHashSet<string> GetSymbolList(
        AnalyzerOptions options,
        SyntaxTree syntaxTree,
        string key)
    {
        var optionsProvider = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);

        if (!optionsProvider.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return ImmutableHashSet<string>.Empty;
        }

        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var entries = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            var trimmed = entry.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                builder.Add(trimmed);
            }
        }

        return builder.ToImmutable();
    }
}
