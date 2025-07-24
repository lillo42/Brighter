using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Paramore.Brighter.Analyzers;

#pragma warning disable RS1038
[DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1038
public class SuggestImplementMapperAsyncAndSyncAnalyzer : DiagnosticAnalyzer
{
    public const string SuggestedInterfaceKey = "BrighterSuggestedInterfaceKey";
    
    public const string SyncDiagnosticId = "BRT0001";
    public const string AsyncDiagnosticId = "BRT0002";
    private const string Category = "Design";
    private static readonly LocalizableString s_title = "Implement IAmAMessageMapper/IAmAMessageMapperAsync";
    private static readonly LocalizableString s_messageFormatSyncToAsync = "Class '{0}' implements 'IAmAMessageMapper<{1}>', consider implementing 'IAmAMessageMapperAsync<{1}>' as well";
    private static readonly LocalizableString s_messageFormatAsyncToSync = "Class '{0}' implements 'IAmAMessageMapperAsync<{1}>', consider implementing 'IAmAMessageMapper<{1}>' as well";
    private static readonly LocalizableString s_description = "Allow Post a message using sync and async method.";

    private static readonly DiagnosticDescriptor s_sRuleSyncToAsync = new(SyncDiagnosticId, s_title, s_messageFormatSyncToAsync, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: s_description);
    private static readonly DiagnosticDescriptor s_sRuleAsyncToSync = new(AsyncDiagnosticId, s_title, s_messageFormatAsyncToSync, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: s_description);
 
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_sRuleSyncToAsync, s_sRuleAsyncToSync];
    
    public override void Initialize(AnalysisContext context)
    {
        // Configure analysis options for performance
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        // Register an action to be performed on NamedType symbols (classes, structs, interfaces, enums)
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }
    
    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        // Cast the symbol to an INamedTypeSymbol (represents a type declaration)
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        // We are only interested in classes
        if (namedTypeSymbol.TypeKind != TypeKind.Class)
        {
            return;
        }

        // Get the INamedTypeSymbol for IInterfaceA and IInterfaceB from the compilation
        // IMPORTANT: Replace "MyApplication.Interfaces.IInterfaceA" and "MyApplication.Interfaces.IInterfaceB"
        // with the actual fully qualified names of your interfaces.
        var messageMapper = context.Compilation.GetTypeByMetadataName("Paramore.Brighter.IAmAMessageMapper`1");
        var messageMapperAsync = context.Compilation.GetTypeByMetadataName("Paramore.Brighter.IAmAMessageMapperAsync`1");

        // If either interface symbol cannot be found, skip analysis
        if (messageMapper == null || messageMapperAsync == null)
        {
            return;
        }

        var messageMapperInterface = namedTypeSymbol.AllInterfaces.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, messageMapper));
        var messageMapperAsyncInterface = namedTypeSymbol.AllInterfaces.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, messageMapperAsync));

        if (messageMapperInterface != null && messageMapperAsyncInterface == null)
        {
            var typeArgName = string.Join(", ", messageMapperInterface.TypeArguments.Select(t => t.ToDisplayString()));
            var diagnostic = Diagnostic.Create(s_sRuleSyncToAsync, namedTypeSymbol.Locations[0], 
                ImmutableDictionary<string, string?>.Empty.Add(SuggestedInterfaceKey, $"Paramore.Brighter.IAmAMessageMapperAsync<{typeArgName}>"), 
                namedTypeSymbol.Name, typeArgName);
            context.ReportDiagnostic(diagnostic);   
        }
        else if (messageMapperInterface == null && messageMapperAsyncInterface != null)
        {
            var typeArgName = string.Join(", ", messageMapperAsyncInterface.TypeArguments.Select(t => t.ToDisplayString()));
            var diagnostic = Diagnostic.Create(s_sRuleAsyncToSync, 
                namedTypeSymbol.Locations[0],
               ImmutableDictionary<string, string?>.Empty.Add(SuggestedInterfaceKey, $"Paramore.Brighter.IAmAMessageMapper<{typeArgName}>"), 
                namedTypeSymbol.Name, typeArgName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
