using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using RDCore.SDK.Extensibility.Diagnostics;
using RDCore.SDK.Model.AST;
using RDCore.SDK.Model.AST.Declarations;
using RDCore.SDK.Model.AST.Expressions;
using RDCore.SDK.Model.Source;
using RDCore.SDK.Platform.Protocol;
using RDCore.SDK.Semantics;
using RDCore.Web.Endpoints;
using RDCore.Web.Services;

namespace RDCore.Web.Diagnostics;

/// <summary>
/// <c>RDW00006</c> (<see cref="RDWebDiagnostics.NonObjectFactoryParameter"/>): a parameter of the factory of a controller or a service is not of an object
/// type.
/// </summary>
/// <remarks>
/// <para>
/// A controller class with a default instance (<c>'@Controller</c>) is made for each request by its factory, the function <c>'@ControllerFactory</c> names
/// or <see cref="RDWebServiceReader.DefaultFactory"/>; a service class with one (<c>'@Service</c>) is made by its
/// <see cref="RDWebServiceReader.DefaultFactory"/>.
/// The factory runs on the default instance, and each of its parameters is given the service its declared type names: a service is an object, so a
/// parameter of any other type is given nothing, and the host refuses the program. It is reported at each such parameter, as an error.
/// </para>
/// <para>
/// A parameter is of an object type when it is declared as a class, an interface or <c>Object</c>, and is not an array. With the host's facts of the module, its
/// declared type is the resolved one (<see cref="DeclaredTypeKind"/>); a type that did not resolve is not reported. Without them, it is read from the
/// declaration: a parameter with no <c>As</c> clause (a <c>Variant</c>), a type character, an intrinsic type, an array or a <c>ParamArray</c> is reported.
/// </para>
/// </remarks>
public sealed class NonObjectFactoryParameterAnalyzer : IModuleAnalyzer
{
    // the intrinsic types of MS-VBAL §2.1, as a declaration names them; a String * n is a String.
    private static readonly HashSet<string> IntrinsicTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Boolean", "Byte", "Integer", "Long", "LongLong", "LongPtr", "Single", "Double", "Currency", "Decimal", "Date", "String", "Variant", "Any",
    };

    /// <inheritdoc/>
    public IReadOnlyList<RDCoreDiagnosticCode> Codes => [RDWebDiagnostics.NonObjectFactoryParameter];

    /// <inheritdoc/>
    public IEnumerable<AnalyzerFinding> Analyze(ModuleAnalysisContext context)
    {
        if (context.ParseResult.SyntaxTree is not { } module || !module.IsPredeclared() || FactoryNameOf(context.ParseResult) is not { } factoryName)
        {
            yield break;
        }

        var factory = module.Children.OfType<MemberDeclarationNode>().FirstOrDefault(member => member.MemberKind is MemberKind.Function
            && string.Equals(member.Name, factoryName, StringComparison.OrdinalIgnoreCase));
        if (factory is null)
        {
            yield break;
        }

        foreach (var parameter in factory.Children.OfType<ParameterDeclarationNode>().Where(parameter => !IsObject(context, factory, parameter)))
        {
            yield return new AnalyzerFinding(RDWebDiagnostics.NonObjectFactoryParameter, parameter.NameRange ?? parameter.SourceLocation.Range,
                DiagnosticSeverity.Error, AnalyzerMessages.Format(RDWebResources.NonObjectFactoryParameter_Message, parameter.Name, factory.Name));
        }
    }

    // the factory of a controller ('@ControllerFactory, or Create) or of a service (Create); none for a module that is neither.
    private static string? FactoryNameOf(ModuleParseResult parse)
    {
        var annotations = parse.GetModuleAnnotations();
        if (annotations.Any(annotation => annotation.IsNamed(RDWebAnnotations.Controller)))
        {
            var factory = annotations.FirstOrDefault(annotation => annotation.IsNamed(RDWebAnnotations.ControllerFactory))?.GetStringArgument();
            return factory is { Length: > 0 } ? factory : RDWebServiceReader.DefaultFactory;
        }

        return annotations.Any(annotation => annotation.IsNamed(RDWebAnnotations.Service)) ? RDWebServiceReader.DefaultFactory : null;
    }

    private static bool IsObject(ModuleAnalysisContext context, MemberDeclarationNode factory, ParameterDeclarationNode parameter)
    {
        if (parameter.IsParamArray || parameter.IsArray)
        {
            return false;
        }

        var fact = context.Semantics?.Declarations.FirstOrDefault(declaration => declaration.Kind is DeclarationKind.Parameter
            && string.Equals(declaration.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)
            && Contains(factory.SourceLocation.Range, declaration.Location.Range.Start));
        if (fact is not null)
        {
            return (fact.TypeKind is null or DeclaredTypeKind.Object or DeclaredTypeKind.Class) && !fact.IsArray;
        }

        var declared = parameter.Children.OfType<AsTypeExpressionNode>().FirstOrDefault();
        return parameter.TypeHint is null && declared is { IsArrayDef: false, FixedLength: null }
            && !(IntrinsicTypes.Contains(declared.TypeName)
                && (declared.QualifierName is null || string.Equals(declared.QualifierName, "VBA", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool Contains(SourceRange range, SourcePosition position)
        => (position.Line > range.Start.Line || position.Line == range.Start.Line && position.Character >= range.Start.Character)
            && (position.Line < range.End.Line || position.Line == range.End.Line && position.Character <= range.End.Character);
}
