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

namespace Seams.Analyzers.CodeFixes.InheritanceBlockers;

/// <summary>
/// Code fix provider that adds the virtual modifier to methods.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonVirtualMethodCodeFix))]
[Shared]
public sealed class NonVirtualMethodCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.NonVirtualMethod);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        var methodDeclaration = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add virtual modifier",
                createChangedDocument: c => AddVirtualModifierAsync(context.Document, methodDeclaration, c),
                equivalenceKey: "AddVirtualModifier"),
            diagnostic);
    }

    private static async Task<Document> AddVirtualModifierAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Find the position to insert virtual (after public/protected, before return type)
        var publicIndex = methodDeclaration.Modifiers.IndexOf(SyntaxKind.PublicKeyword);
        var protectedIndex = methodDeclaration.Modifiers.IndexOf(SyntaxKind.ProtectedKeyword);
        var insertIndex = System.Math.Max(publicIndex, protectedIndex) + 1;

        var virtualToken = SyntaxFactory.Token(SyntaxKind.VirtualKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        var newModifiers = methodDeclaration.Modifiers.Insert(insertIndex, virtualToken);
        var newMethodDeclaration = methodDeclaration.WithModifiers(newModifiers);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}
