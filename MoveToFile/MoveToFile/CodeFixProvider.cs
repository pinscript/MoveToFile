using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoveToFile
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveToFileCodeFixProvider)), Shared]
    public class MoveToFileCodeFixProvider : CodeFixProvider
    {
        private const string TitleFormat = "Move {0} to {0}.cs";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(MoveToFileAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var syntaxRoot = (CompilationUnitSyntax)root;
                        
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
            var identifierName = declaration.Identifier.Text;

            var title = string.Format(TitleFormat, identifierName);
                
            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => MoveSymbolToFile(context.Document, syntaxRoot, declaration),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> MoveSymbolToFile(Document document, CompilationUnitSyntax syntaxRoot, TypeDeclarationSyntax typeDecl)
        {
            var identifierToken = typeDecl.Identifier;
            var originalSolution = document.Project.Solution;

            // Get the typeDecl's namespace
            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = semanticModel.GetDeclaredSymbol(typeDecl);

            // Build new file
            var newFileTree = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(syntaxRoot.Usings))
                .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(symbol.ContainingNamespace.Name))
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(typeDecl))))
                .WithoutLeadingTrivia()
                .NormalizeWhitespace();

            var newDocumentId = DocumentId.CreateNewId(document.Project.Id);
            var newSolution = originalSolution.AddDocument(newDocumentId, identifierToken.Text, newFileTree, document.Folders);

            // Remove declaration from source file
            var syntaxTree = await document.GetSyntaxTreeAsync();
            var modifiedSyntaxTree = syntaxTree.GetRoot().RemoveNode(typeDecl, SyntaxRemoveOptions.KeepNoTrivia);

            // If the original file now has no types, remove it completely.
            var remainingTypesInDocument = modifiedSyntaxTree.DescendantNodesAndSelf()?
                                                             .Where(x => x.GetType() == typeof(EnumDeclarationSyntax) ||
                                                                         x.GetType() == typeof(TypeDeclarationSyntax)).ToList();
            if(remainingTypesInDocument == null || !remainingTypesInDocument.Any())
            {
                // Remove this file completely
                newSolution = newSolution.RemoveDocument(document.Id);
            } else
            {
                // Include the modified document
                newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, modifiedSyntaxTree);
            }

            // TOODO: Remove unused usings from both source and new.
            
            return newSolution;
        }
    }
}