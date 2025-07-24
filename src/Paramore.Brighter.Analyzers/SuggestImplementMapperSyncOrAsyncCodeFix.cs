using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Paramore.Brighter.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SuggestImplementMapperAsyncAndSyncAnalyzer)), Shared]
public class SuggestImplementMapperSyncOrAsyncCodeFix : CodeFixProvider
{
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [SuggestImplementMapperAsyncAndSyncAnalyzer.SyncDiagnosticId];
    
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Properties.TryGetValue(SuggestImplementMapperAsyncAndSyncAnalyzer.SuggestedInterfaceKey, out var suggestedInterface))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Add interface '{suggestedInterface}'",
                        createChangedDocument: c => 
                            AddInterfaceAsync(context.Document, context, suggestedInterface!),
                        equivalenceKey: $"Add interface '{suggestedInterface}'"),
                    diagnostic);
            }
        }
        
        return Task.CompletedTask;
    }

    private static async Task<Document> AddInterfaceAsync(Document document, CodeFixContext context, string interfaceName)
    {
        var root = await context.Document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var classDecl = root!.FindToken(diagnosticSpan.Start)
            .Parent!.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

        // Create new base type syntax
        var newInterface = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(interfaceName));

        // Add to existing base list or create new one
        var baseList = classDecl.BaseList ?? SyntaxFactory.BaseList(SyntaxFactory.SeparatedList<BaseTypeSyntax>());

        var newBaseList = baseList.WithTypes(baseList.Types.Add(newInterface));

        // Update class declaration
        var newClassDecl = classDecl.WithBaseList(newBaseList);
        var newRoot = root.ReplaceNode(classDecl, newClassDecl);
        
        return document.WithSyntaxRoot(newRoot);
    }
}
