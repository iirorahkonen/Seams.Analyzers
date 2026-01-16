using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestHarness.Analyzers;

/// <summary>
/// Extension methods for syntax node analysis to reduce code duplication across analyzers.
/// </summary>
public static class SyntaxNodeExtensions
{
    /// <summary>
    /// Checks if the node is inside a class matching any of the specified predicates.
    /// </summary>
    public static bool IsInClassMatching(
        this SyntaxNode node,
        Func<ClassDeclarationSyntax, bool> classPredicate)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is ClassDeclarationSyntax classDecl && classPredicate(classDecl))
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    /// <summary>
    /// Checks if the node is inside a method matching the specified predicate.
    /// </summary>
    public static bool IsInMethodMatching(
        this SyntaxNode node,
        Func<MethodDeclarationSyntax, bool> methodPredicate)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is MethodDeclarationSyntax methodDecl && methodPredicate(methodDecl))
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    /// <summary>
    /// Checks if the node is inside a composition root context (Startup, Program, ConfigureServices, etc.).
    /// </summary>
    public static bool IsInCompositionRoot(this SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is ClassDeclarationSyntax classDecl)
            {
                var className = classDecl.Identifier.Text;
                if (className is "Startup" or "Program" or "CompositionRoot" or
                    "ServiceCollectionExtensions" or "DependencyInjectionExtensions")
                {
                    return true;
                }
            }
            else if (current is MethodDeclarationSyntax methodDecl)
            {
                var methodName = methodDecl.Identifier.Text;
                if (methodName is "ConfigureServices" or "AddServices" or "RegisterServices" or
                    "Configure" or "ConfigureContainer")
                {
                    return true;
                }
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks if the node is inside a factory delegate (lambda in AddSingleton/AddScoped/etc.).
    /// </summary>
    public static bool IsInFactoryDelegate(this SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            // Check if we're inside a lambda or anonymous method
            if (current is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
            {
                // Check if the lambda is an argument to AddSingleton, AddScoped, etc.
                if (current.Parent is ArgumentSyntax arg && arg.Parent is ArgumentListSyntax argList)
                {
                    if (argList.Parent is InvocationExpressionSyntax invocation &&
                        invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                    {
                        var methodName = memberAccess.Name.Identifier.Text;
                        if (methodName.StartsWith("Add", StringComparison.Ordinal) ||
                            methodName is "Register" or "RegisterType" or "RegisterInstance")
                        {
                            return true;
                        }
                    }
                }
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks if the node is inside a data access context (Repository, DataAccess, DbContext classes).
    /// </summary>
    public static bool IsInDataAccessContext(this SyntaxNode node)
    {
        return node.IsInClassMatching(classDecl =>
        {
            var className = classDecl.Identifier.Text;
            return className.EndsWith("Repository", StringComparison.Ordinal) ||
                   className.EndsWith("DataAccess", StringComparison.Ordinal) ||
                   className.Contains("DbContext") ||
                   className.Contains("ConnectionFactory") ||
                   className.Contains("ConnectionPool");
        });
    }

    /// <summary>
    /// Checks if the node is inside an HTTP client factory context.
    /// </summary>
    public static bool IsInHttpClientFactoryContext(this SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            // Check if we're in a class that implements IHttpClientFactory
            if (current is ClassDeclarationSyntax classDecl)
            {
                var className = classDecl.Identifier.Text;
                if (className.Contains("Factory") || className.Contains("HttpClient"))
                {
                    return true;
                }
            }

            // Check if we're in a method that creates HttpClient for factory purposes
            if (current is MethodDeclarationSyntax methodDecl)
            {
                var methodName = methodDecl.Identifier.Text;
                if (methodName is "CreateClient" or "CreateHttpClient" or "Build")
                {
                    return true;
                }
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks if the node is inside a static readonly field initializer.
    /// </summary>
    public static bool IsInStaticReadonlyFieldInitializer(this SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is FieldDeclarationSyntax field)
            {
                return field.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword) &&
                       field.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ReadOnlyKeyword);
            }

            if (current is MethodDeclarationSyntax or
                ConstructorDeclarationSyntax or
                PropertyDeclarationSyntax)
            {
                return false;
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks if the node is inside a test class (based on naming or attributes).
    /// </summary>
    public static bool IsInTestClass(this SyntaxNode node)
    {
        return node.IsInClassMatching(classDecl =>
        {
            var className = classDecl.Identifier.Text;
            return className.EndsWith("Tests", StringComparison.Ordinal) ||
                   className.EndsWith("Test", StringComparison.Ordinal) ||
                   className.StartsWith("Test", StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// Gets all ancestor nodes of the specified type.
    /// </summary>
    public static IEnumerable<T> GetAncestors<T>(this SyntaxNode node) where T : SyntaxNode
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is T typedNode)
            {
                yield return typedNode;
            }
            current = current.Parent;
        }
    }
}
