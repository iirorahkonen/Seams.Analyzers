using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHarness.Analyzers.Analyzers.Infrastructure;

/// <summary>
/// Analyzer that detects direct database connection creation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DatabaseAccessAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> DatabaseConnectionTypes = ImmutableHashSet.Create(
        System.StringComparer.Ordinal,
        "System.Data.SqlClient.SqlConnection",
        "Microsoft.Data.SqlClient.SqlConnection",
        "System.Data.OleDb.OleDbConnection",
        "System.Data.Odbc.OdbcConnection",
        "Npgsql.NpgsqlConnection",
        "MySql.Data.MySqlClient.MySqlConnection",
        "MySqlConnector.MySqlConnection",
        "Microsoft.Data.Sqlite.SqliteConnection",
        "System.Data.SQLite.SQLiteConnection",
        "Oracle.ManagedDataAccess.Client.OracleConnection",
        "Oracle.DataAccess.Client.OracleConnection");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DatabaseAccess);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        AnalyzeType(context, objectCreation, typeInfo.Type);
    }

    private static void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var implicitCreation = (ImplicitObjectCreationExpressionSyntax)context.Node;

        var typeInfo = context.SemanticModel.GetTypeInfo(implicitCreation, context.CancellationToken);
        AnalyzeType(context, implicitCreation, typeInfo.Type);
    }

    private static void AnalyzeType(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax creationExpression,
        ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol namedType)
            return;

        var fullTypeName = namedType.ToDisplayString();

        // Check for known database connection types
        if (!DatabaseConnectionTypes.Contains(fullTypeName) && !IsDbConnectionSubtype(namedType))
            return;

        // Skip if in a repository or data access class
        if (creationExpression.IsInDataAccessContext())
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.DatabaseAccess,
            creationExpression.GetLocation(),
            namedType.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsDbConnectionSubtype(INamedTypeSymbol type)
    {
        // Check if the type inherits from DbConnection
        var baseType = type.BaseType;
        while (baseType != null)
        {
            var fullName = baseType.ToDisplayString();
            if (fullName == "System.Data.Common.DbConnection" ||
                fullName == "System.Data.IDbConnection")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        // Check interfaces
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.ToDisplayString() == "System.Data.IDbConnection")
                return true;
        }

        return false;
    }
}
