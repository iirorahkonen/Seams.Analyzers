using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestHarness.Analyzers.CodeFixes.StaticDependencies;

/// <summary>
/// Code fix provider that replaces DateTime.Now/UtcNow with TimeProvider injection.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DateTimeNowCodeFix))]
[Shared]
public sealed class DateTimeNowCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DateTimeNow);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        var memberAccess = node.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
        if (memberAccess == null)
            return;

        // Offer TimeProvider for .NET 8+
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Inject TimeProvider",
                createChangedDocument: c => InjectTimeProviderAsync(context.Document, memberAccess, c),
                equivalenceKey: "InjectTimeProvider"),
            diagnostic);

        // Offer ITimeProvider interface for older .NET versions
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Inject ITimeProvider (custom interface)",
                createChangedDocument: c => InjectCustomTimeProviderAsync(context.Document, memberAccess, c),
                equivalenceKey: "InjectCustomTimeProvider"),
            diagnostic);
    }

    private static async Task<Document> InjectTimeProviderAsync(
        Document document,
        MemberAccessExpressionSyntax memberAccess,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Determine the property being accessed
        var propertyName = memberAccess.Name.Identifier.Text;
        var isUtc = propertyName is "UtcNow" or "Today";

        // Find the containing class
        var containingClass = memberAccess.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass == null)
            return document;

        const string fieldName = "_timeProvider";
        const string parameterName = "timeProvider";
        const string typeName = "TimeProvider";

        // Check if field already exists
        var fieldExists = containingClass.Members
            .OfType<FieldDeclarationSyntax>()
            .Any(f => f.Declaration.Variables.Any(v => v.Identifier.Text == fieldName));

        var newClass = containingClass;

        if (!fieldExists)
        {
            // Create the field declaration
            var fieldDeclaration = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(typeName))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(fieldName)))))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));

            // Find or create constructor
            var existingConstructor = containingClass.Members
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword));

            if (existingConstructor != null)
            {
                // Check if parameter already exists
                var paramExists = existingConstructor.ParameterList.Parameters
                    .Any(p => p.Identifier.Text == parameterName);

                if (!paramExists)
                {
                    var newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                        .WithType(SyntaxFactory.ParseTypeName(typeName));

                    var newParameterList = existingConstructor.ParameterList.AddParameters(newParameter);

                    var assignmentStatement = SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(fieldName),
                            SyntaxFactory.IdentifierName(parameterName)));

                    var statements = existingConstructor.Body?.Statements.Insert(0, assignmentStatement)
                        ?? SyntaxFactory.SingletonList<StatementSyntax>(assignmentStatement);

                    var newConstructor = existingConstructor
                        .WithParameterList(newParameterList)
                        .WithBody(SyntaxFactory.Block(statements));

                    newClass = containingClass
                        .ReplaceNode(existingConstructor, newConstructor)
                        .WithMembers(containingClass.Members.Insert(0, fieldDeclaration));
                }
            }
            else
            {
                var constructorParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                    .WithType(SyntaxFactory.ParseTypeName(typeName));

                var assignmentStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(fieldName),
                        SyntaxFactory.IdentifierName(parameterName)));

                var newConstructor = SyntaxFactory.ConstructorDeclaration(containingClass.Identifier)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(constructorParameter)))
                    .WithBody(SyntaxFactory.Block(assignmentStatement));

                newClass = containingClass.WithMembers(
                    containingClass.Members.Insert(0, fieldDeclaration).Insert(1, newConstructor));
            }
        }

        // Replace the DateTime.Now with _timeProvider.GetUtcNow() or similar
        var replacement = isUtc
            ? SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.IdentifierName("GetUtcNow")))
            : SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    SyntaxFactory.IdentifierName("GetLocalNow")));

        // Find the member access in the new class and replace
        var oldMemberAccessInNewClass = newClass.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .FirstOrDefault(m => m.ToString() == memberAccess.ToString());

        if (oldMemberAccessInNewClass != null)
        {
            newClass = newClass.ReplaceNode(oldMemberAccessInNewClass, replacement);
        }

        var newRoot = root.ReplaceNode(containingClass, newClass);
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> InjectCustomTimeProviderAsync(
        Document document,
        MemberAccessExpressionSyntax memberAccess,
        CancellationToken cancellationToken)
    {
        // Similar implementation but uses ITimeProvider interface
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // For now, just add a comment suggesting the pattern
        var comment = SyntaxFactory.Comment("// TODO: Inject ITimeProvider and use it here");
        var newMemberAccess = memberAccess.WithLeadingTrivia(
            memberAccess.GetLeadingTrivia().Add(comment).Add(SyntaxFactory.ElasticCarriageReturnLineFeed));

        var newRoot = root.ReplaceNode(memberAccess, newMemberAccess);
        return document.WithSyntaxRoot(newRoot);
    }
}
