using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using RDCore.SDK.Extensibility.Diagnostics;
using RDCore.SDK.Model.AST;
using RDCore.SDK.Model.Source;
using RDCore.SDK.Platform.Protocol;
using RDCore.Web.Endpoints;
using RDCore.Web.Services;

namespace RDCore.Web.Diagnostics;

/// <summary>
/// <c>RDW00005</c> (<see cref="RDWebDiagnostics.DuplicateServiceName"/>): two classes of a project declare one service name with <c>'@Service</c>.
/// </summary>
/// <remarks>
/// A service's name is the type a parameter declares to be given it, so a parameter declared as that type could be given an object of either class: the host
/// refuses the program (<see cref="RDWebServiceReader.Distinct"/>). It is reported at the <c>'@Service</c> of each class that declares the name, as an error,
/// with the others' as related locations. Names are compared without regard to case, as VBA names are (<strong>MS-VBAL §3.3.5.1</strong>). A
/// <c>'@Service</c> that the host refuses for another reason (<see cref="RDWebServiceException"/>) declares no name here.
/// </remarks>
public sealed class DuplicateServiceNameAnalyzer : IProjectModulesAnalyzer
{
    /// <inheritdoc/>
    public IReadOnlyList<RDCoreDiagnosticCode> Codes => [RDWebDiagnostics.DuplicateServiceName];

    /// <inheritdoc/>
    public IEnumerable<AnalyzerFinding> Analyze(ProjectModulesAnalysisContext context)
    {
        var declared = context.Modules.Select(Declared).OfType<Declaration>().ToArray();
        foreach (var name in declared.GroupBy(declaration => declaration.Service.Name, StringComparer.OrdinalIgnoreCase).Where(name => name.Count() > 1))
        {
            foreach (var declaration in name)
            {
                var others = name.Where(other => other != declaration).ToArray();
                yield return new AnalyzerFinding(RDWebDiagnostics.DuplicateServiceName, declaration.Range, DiagnosticSeverity.Error,
                    AnalyzerMessages.Format(RDWebResources.DuplicateServiceName_Message, declaration.Service.Name,
                        string.Join(", ", others.Select(other => other.Service.ClassName))))
                {
                    Document = declaration.Module.Document,
                    Related = [.. others.Select(other => new RelatedFinding(other.Module.Document, other.Range,
                        AnalyzerMessages.Format(RDWebResources.DuplicateServiceName_Related, other.Service.ClassName)))],
                };
            }
        }
    }

    private sealed record class Declaration(ModuleAnalysisContext Module, RDWebService Service, SourceRange Range);

    // the service a module declares, at its '@Service; none for a module that declares none, or whose '@Service the host refuses for another reason.
    private static Declaration? Declared(ModuleAnalysisContext module)
    {
        if (module.ParseResult.GetModuleAnnotations().FirstOrDefault(annotation => annotation.IsNamed(RDWebAnnotations.Service)) is not { } annotation)
        {
            return null;
        }

        try
        {
            return RDWebServiceReader.Read(module.ParseResult, module.ModuleName, module.ModuleKind) is { } service
                ? new Declaration(module, service, annotation.SourceLocation.Range)
                : null;
        }
        catch (RDWebServiceException)
        {
            return null;
        }
    }
}
