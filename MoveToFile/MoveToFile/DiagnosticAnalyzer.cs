using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MoveToFile
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MoveToFileAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MoveToFile";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        // TODO: Is there a fixed set of categories?
        private const string Category = "Files";

        private static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }
        
        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // Check that the symbols name is the same as the current file name
            // Files with multiple locations (eg partials) are ignored for now.

            var symbol = context.Symbol;
            var locations = symbol.Locations;
            if(!locations.Any() || locations.Count() > 1)
            {
                return;
            }

            var filePath = symbol.Locations.First().SourceTree.FilePath;
            var isOk = filePath.EndsWith(symbol.Name + ".cs");

            // File does not match symbol name, produce a diagnostic.
            if (!isOk)
            {
                var diagnostic = Diagnostic.Create(Rule, locations[0], symbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
