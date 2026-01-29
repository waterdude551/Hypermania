using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Hypermania.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoFloatDoubleInGameSimAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "HM0001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Disallow float/double in Game.Sim",
        messageFormat: "Type '{0}' is not allowed in namespace Game.Sim",
        category: "Determinism",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);
        context.RegisterSyntaxNodeAction(
            AnalyzeNumericLiteral,
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression
        );
        context.RegisterSyntaxNodeAction(
            AnalyzeCastExpression,
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.CastExpression
        );

        context.RegisterSyntaxNodeAction(
            AnalyzeLocalDeclaration,
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.LocalDeclarationStatement
        );
    }

    private static bool IsInTargetNamespace(ISymbol symbol)
    {
        var ns = symbol.ContainingNamespace?.ToDisplayString() ?? "";
        return ns == "Game.Sim" || ns.StartsWith("Game.Sim.");
    }

    private static bool IsBanned(ITypeSymbol? type) =>
        type?.SpecialType is SpecialType.System_Single or SpecialType.System_Double;

    private static bool IsSfloat(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        return type.ToDisplayString() == "Utils.SoftFloat.sfloat";
    }

    private static bool IsImmediatelyCastedToSfloat(
        SyntaxNode node,
        SyntaxNodeAnalysisContext context
    )
    {
        SyntaxNode current = node;

        while (current.Parent is ParenthesizedExpressionSyntax)
            current = current.Parent;

        if (current.Parent is not CastExpressionSyntax cast)
            return false;

        var castType = context.SemanticModel.GetTypeInfo(cast.Type, context.CancellationToken).Type;
        return IsSfloat(castType);
    }

    private static void Report(SymbolAnalysisContext context, Location location, ITypeSymbol type)
    {
        context.ReportDiagnostic(
            Diagnostic.Create(
                Rule,
                location,
                type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            )
        );
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (!IsInTargetNamespace(field))
            return;
        if (IsBanned(field.Type))
            Report(context, field.Locations[0], field.Type);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var prop = (IPropertySymbol)context.Symbol;
        if (!IsInTargetNamespace(prop))
            return;
        if (IsBanned(prop.Type))
            Report(context, prop.Locations[0], prop.Type);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (!IsInTargetNamespace(method))
            return;

        if (IsBanned(method.ReturnType))
            Report(context, method.Locations[0], method.ReturnType);

        foreach (var p in method.Parameters)
        {
            if (IsBanned(p.Type))
                Report(
                    context,
                    p.Locations.Length > 0 ? p.Locations[0] : method.Locations[0],
                    p.Type
                );
        }
    }

    private static void AnalyzeParameter(SymbolAnalysisContext context)
    {
        var p = (IParameterSymbol)context.Symbol;
        if (!IsInTargetNamespace(p))
            return;
        if (IsBanned(p.Type))
            Report(context, p.Locations[0], p.Type);
    }

    private static void AnalyzeLocalDeclaration(SyntaxNodeAnalysisContext context)
    {
        var symbol = context.ContainingSymbol;
        if (symbol is null || !IsInTargetNamespace(symbol))
            return;

        var localDecl = (LocalDeclarationStatementSyntax)context.Node;

        // Handles: float x = ...;  double y = ...;
        // Handles: var x = 1f; (type inferred as float/double)
        foreach (var v in localDecl.Declaration.Variables)
        {
            var localSymbol =
                context.SemanticModel.GetDeclaredSymbol(v, context.CancellationToken)
                as ILocalSymbol;
            if (localSymbol is null)
                continue;

            var type = localSymbol.Type;
            if (IsBanned(type))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        v.Identifier.GetLocation(),
                        type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                    )
                );
            }
        }
    }

    private static void AnalyzeNumericLiteral(SyntaxNodeAnalysisContext context)
    {
        var symbol = context.ContainingSymbol;
        if (symbol is null || !IsInTargetNamespace(symbol))
            return;

        var literal = (LiteralExpressionSyntax)context.Node;

        var type = context.SemanticModel.GetTypeInfo(literal, context.CancellationToken).Type;
        if (!IsBanned(type))
            return;

        // Allow float/double literals only if they are immediately cast to sfloat
        if (IsImmediatelyCastedToSfloat(literal, context))
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rule,
                literal.GetLocation(),
                type!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            )
        );
    }

    private static void AnalyzeCastExpression(SyntaxNodeAnalysisContext context)
    {
        var symbol = context.ContainingSymbol;
        if (symbol is null || !IsInTargetNamespace(symbol))
            return;

        var cast = (CastExpressionSyntax)context.Node;

        // Only ban casts *to* float/double
        var type = context.SemanticModel.GetTypeInfo(cast.Type, context.CancellationToken).Type;
        if (!IsBanned(type))
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rule,
                cast.Type.GetLocation(),
                type!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            )
        );
    }
}
