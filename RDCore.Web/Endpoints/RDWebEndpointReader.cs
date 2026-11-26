using RDCore.SDK.Model;
using RDCore.SDK.Model.AST;
using RDCore.SDK.Model.AST.Abstract;
using RDCore.SDK.Model.AST.Declarations;
using System.Collections.Immutable;

namespace RDCore.Web.Endpoints;

/// <summary>
/// Reads the endpoints a module declares from its annotations: <c>'@Controller</c> in its declarations section, or above the header of its block
/// (<c>Module Name</c>, <c>Class Name</c>) for the module of a block of a file, and <c>'@HttpGet</c> or <c>'@HttpPost</c> immediately above each public
/// <c>Sub</c> or <c>Function</c> that serves one, with the <c>'@FromBody</c> that names its body parameter.
/// </summary>
public static class RDWebEndpointReader
{
    /// <summary>
    /// The endpoints a module declares.
    /// </summary>
    /// <param name="parse">
    /// The module's parse; for a file that has blocks, the parse of the one module (<see cref="SDK.Workspace.RDCoreDocument.ParseOf"/>).
    /// </param>
    /// <param name="moduleName">The module's name.</param>
    /// <returns>
    /// One endpoint for each HTTP method annotation of each public procedure, in the order they are written; none for a module that is not a controller. A
    /// <c>Private</c> or <c>Friend</c> procedure serves no endpoint.
    /// </returns>
    /// <exception cref="RDWebEndpointException">
    /// An annotation declares an endpoint that cannot be served (<see cref="RDWebEndpoint.FromAnnotation"/>).
    /// </exception>
    public static ImmutableArray<RDWebEndpoint> Read(ModuleParseResult parse, string moduleName)
    {
        if (parse.SyntaxTree is not { } module
            || parse.GetModuleAnnotations().FirstOrDefault(annotation => Is(annotation, RDWebAnnotations.Controller)) is not { } controller)
        {
            return [];
        }

        var controllerRoute = controller.GetStringArgument();
        var endpoints = ImmutableArray.CreateBuilder<RDWebEndpoint>();
        foreach (var procedure in module.Children.OfType<MemberDeclarationNode>().Where(IsPublicProcedure))
        {
            var parameters = procedure.Children.OfType<ParameterDeclarationNode>().Select(parameter => parameter.Name).ToArray();
            var annotations = parse.GetAnnotations(procedure);
            var body = annotations.FirstOrDefault(annotation => Is(annotation, RDWebAnnotations.FromBody))?.GetStringArgument();
            foreach (var annotation in annotations)
            {
                if (RDWebEndpoint.FromAnnotation(moduleName, controllerRoute, procedure.Name, parameters, annotation.Name, annotation.GetStringArgument(), body)
                    is { } endpoint)
                {
                    endpoints.Add(endpoint);
                }
            }
        }

        return endpoints.ToImmutable();
    }

    private static bool Is(AnnotationTriviaNode annotation, string name) => string.Equals(annotation.Name, name, StringComparison.OrdinalIgnoreCase);

    private static bool IsPublicProcedure(MemberDeclarationNode member)
        => member.MemberKind is MemberKind.Procedure or MemberKind.Function
            && member.AccessModifier is AccessModifier.Implicit or AccessModifier.Public or AccessModifier.Global;
}
