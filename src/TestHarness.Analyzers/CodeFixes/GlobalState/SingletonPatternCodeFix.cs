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

namespace TestHarness.Analyzers.CodeFixes.GlobalState;

/// <summary>
/// Code fix provider that converts singleton pattern to DI-friendly registration.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SingletonPatternCodeFix))]
[Shared]
public sealed class SingletonPatternCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.SingletonPattern);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        var classDeclaration = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Convert to DI-friendly class",
                createChangedDocument: c => ConvertToDiFriendlyAsync(context.Document, classDeclaration, c),
                equivalenceKey: "ConvertToDiFriendly"),
            diagnostic);
    }

    private static async Task<Document> ConvertToDiFriendlyAsync(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var newMembers = new SyntaxList<MemberDeclarationSyntax>();

        foreach (var member in classDeclaration.Members)
        {
            // Remove static Instance property
            if (member is PropertyDeclarationSyntax property &&
                property.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                property.Identifier.Text is "Instance" or "Current" or "Default")
            {
                continue;
            }

            // Remove static instance field
            if (member is FieldDeclarationSyntax field &&
                field.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var fieldName = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "";
                if (fieldName is "_instance" or "instance" or "_current" or "s_instance")
                {
                    continue;
                }
            }

            // Make private constructor public
            if (member is ConstructorDeclarationSyntax constructor)
            {
                if (constructor.Modifiers.Any(SyntaxKind.PrivateKeyword) ||
                    (!constructor.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     !constructor.Modifiers.Any(SyntaxKind.ProtectedKeyword) &&
                     !constructor.Modifiers.Any(SyntaxKind.InternalKeyword)))
                {
                    var newModifiers = SyntaxFactory.TokenList(
                        constructor.Modifiers
                            .Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))
                            .Prepend(SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxFactory.Space)));

                    var newConstructor = constructor.WithModifiers(newModifiers);
                    newMembers = newMembers.Add(newConstructor);
                    continue;
                }
            }

            newMembers = newMembers.Add(member);
        }

        // If no constructor exists after removal, add a public parameterless one
        if (!newMembers.OfType<ConstructorDeclarationSyntax>().Any())
        {
            var newConstructor = SyntaxFactory.ConstructorDeclaration(classDeclaration.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithBody(SyntaxFactory.Block());

            newMembers = newMembers.Insert(0, newConstructor);
        }

        // Add XML comment about DI registration
        var leadingTrivia = classDeclaration.GetLeadingTrivia();
        var commentTrivia = SyntaxFactory.Comment("// Register as singleton in DI: services.AddSingleton<" + classDeclaration.Identifier.Text + ">();");

        var newClassDeclaration = classDeclaration
            .WithMembers(newMembers)
            .WithLeadingTrivia(leadingTrivia.Add(commentTrivia).Add(SyntaxFactory.ElasticCarriageReturnLineFeed));

        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}
