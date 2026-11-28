using RDCore.SDK.Extensibility.Annotations;
using RDCore.Web.Endpoints;

namespace RDCore.Web.Host;

// the annotations of the RDWeb extension, which `rdc describe-ext` writes into its manifest (ExtensionAnnotationsAttribute, in Program.cs): completion and the
// platform's checks of where an annotation is legal know them without starting the extension. What each one does is RDWebEndpointReader's.
internal sealed class RDWebAnnotationDeclarations : IAnnotationDeclarations
{
    // the base name of the resources that describe them: AnnotationDescriptions.resx and its .fr.resx.
    public const string DescriptionsResource = "RDCore.Web.Host.AnnotationDescriptions";

    // a controller is a standard module or a class module (rdweb.2.0.annotations.md).
    private const AnnotationModuleKinds ControllerModules = AnnotationModuleKinds.Standard | AnnotationModuleKinds.Class;

    public IReadOnlyList<AnnotationDeclaration> Declarations { get; } =
    [
        new()
        {
            Name = RDWebAnnotations.Controller, Targets = AnnotationTargets.Module, ModuleKinds = ControllerModules,
            Parameters = [Optional("Route")],
        },
        new()
        {
            Name = RDWebAnnotations.KeptMachine, Targets = AnnotationTargets.Module, ModuleKinds = ControllerModules,
            Parameters = [Required("Key")],
        },
        // a procedure serves an endpoint for each of its HTTP method annotations, so each may be written more than once, with another route.
        new()
        {
            Name = RDWebAnnotations.HttpGet, Targets = AnnotationTargets.Member, ModuleKinds = ControllerModules, Repeats = true,
            Parameters = [Optional("Route")],
        },
        new()
        {
            Name = RDWebAnnotations.HttpPost, Targets = AnnotationTargets.Member, ModuleKinds = ControllerModules, Repeats = true,
            Parameters = [Optional("Route")],
        },
        new()
        {
            Name = RDWebAnnotations.FromBody, Targets = AnnotationTargets.Member, ModuleKinds = ControllerModules,
            Parameters = [Required("Parameter")],
        },
        new() { Name = RDWebAnnotations.NewMachine, Targets = AnnotationTargets.Member, ModuleKinds = ControllerModules },
        // who may reach a controller, or one of its actions.
        new()
        {
            Name = RDWebAnnotations.Authorize, Targets = AnnotationTargets.Module | AnnotationTargets.Member, ModuleKinds = ControllerModules,
            Parameters = [Optional("Policy")],
        },
        new() { Name = RDWebAnnotations.AllowAnonymous, Targets = AnnotationTargets.Module | AnnotationTargets.Member, ModuleKinds = ControllerModules },
        // a factory runs on a default instance, and a service is an object: both are of a class module only.
        new()
        {
            Name = RDWebAnnotations.ControllerFactory, Targets = AnnotationTargets.Module, ModuleKinds = AnnotationModuleKinds.Class,
            Parameters = [Required("Factory")],
        },
        new()
        {
            Name = RDWebAnnotations.Service, Targets = AnnotationTargets.Module, ModuleKinds = AnnotationModuleKinds.Class,
            Parameters = [Optional("Name"), Optional("Lifetime")],
        },
    ];

    private static AnnotationParameter Required(string name) => new() { Name = name, Takes = AnnotationArgumentKind.Text };

    private static AnnotationParameter Optional(string name) => new() { Name = name, Takes = AnnotationArgumentKind.Text, IsOptional = true };
}
