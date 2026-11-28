using RDCore.SDK.Model.AST;
using RDCore.SDK.Model.AST.Declarations;
using RDCore.Web.Endpoints;
using System.Collections.Immutable;
using System.Globalization;

namespace RDCore.Web.Services;

/// <summary>
/// Reads the services of a program from the <c>'@Service</c> annotations of its classes: <c>'@Service</c>, <c>'@Service("IOrderRepository")</c>,
/// <c>'@Service("IClock", "Singleton")</c>, in a class's declarations section.
/// </summary>
/// <remarks>
/// A service's name is the type a parameter declares to be given it: the annotation's first argument, or the class's own name without one. The class is that
/// type, or names it in an <c>Implements</c> statement (<strong>MS-VBAL §5.2.4.2</strong>). Its lifetime is the second argument, <c>Scoped</c>,
/// <c>Transient</c> or <c>Singleton</c> in any case, and <c>Scoped</c> without one. Names are compared without regard to case, as VBA names are
/// (<strong>MS-VBAL §3.3.5.1</strong>).
/// </remarks>
public static class RDWebServiceReader
{
    /// <summary>
    /// The function that makes the object of a class with a default instance, a service or a factory-made controller, when no annotation names another:
    /// <c>Create</c>. It runs on the default instance, and its parameters are services.
    /// </summary>
    public const string DefaultFactory = "Create";

    /// <summary>
    /// The service a module declares.
    /// </summary>
    /// <param name="parse">The module's parse; for a file that has blocks, the parse of the one module.</param>
    /// <param name="moduleName">The module's name.</param>
    /// <param name="moduleType">The module's kind: only a class module declares a service.</param>
    /// <returns>The service; <see langword="null"/> when the module has no <c>'@Service</c>.</returns>
    /// <exception cref="RDWebServiceException">
    /// The module is not a class module, the lifetime is not one of <see cref="RDWebServiceLifetime"/>, or the class is not the type its name is and names it
    /// in no <c>Implements</c> statement.
    /// </exception>
    public static RDWebService? Read(ModuleParseResult parse, string moduleName, ModuleType moduleType)
    {
        if (parse.SyntaxTree is not { } module
            || parse.GetModuleAnnotations().FirstOrDefault(annotation => annotation.IsNamed(RDWebAnnotations.Service)) is not { } service)
        {
            return null;
        }

        if (moduleType is not ModuleType.ClassModule)
        {
            throw Refused(RDWebResources.Service_NotAClass, moduleName);
        }

        var name = service.GetStringArgument() is { Length: > 0 } written ? written : moduleName;
        var lifetime = RDWebServiceLifetime.Scoped;
        if (service.GetStringArgument(1) is { } writtenLifetime)
        {
            lifetime = Enum.GetValues<RDWebServiceLifetime>()
                .Where(value => string.Equals(value.ToString(), writtenLifetime, StringComparison.OrdinalIgnoreCase))
                .Cast<RDWebServiceLifetime?>()
                .FirstOrDefault() ?? throw Refused(RDWebResources.Service_UnknownLifetime, moduleName, writtenLifetime);
        }

        if (!IsNamed(moduleName, name) && !module.GetImplementedInterfaceNames().Any(implemented => IsNamed(implemented, name)))
        {
            throw Refused(RDWebResources.Service_NotItsType, moduleName, name);
        }

        return new RDWebService(name, lifetime, moduleName, module.IsPredeclared());
    }

    /// <summary>
    /// The services of a program, each name once (<c>RDW00005</c>).
    /// </summary>
    /// <param name="services">The service each class of the program declares (<see cref="Read"/>).</param>
    /// <returns>The services, in the order they are given.</returns>
    /// <exception cref="RDWebServiceException">Two classes declare one service name; the message names both, and the name.</exception>
    public static ImmutableArray<RDWebService> Distinct(IEnumerable<RDWebService> services)
    {
        var byName = new Dictionary<string, RDWebService>(StringComparer.OrdinalIgnoreCase);
        var read = ImmutableArray.CreateBuilder<RDWebService>();
        foreach (var service in services)
        {
            if (byName.TryGetValue(service.Name, out var first))
            {
                throw Refused(RDWebResources.Service_DuplicateName, first.ClassName, service.ClassName, service.Name);
            }

            byName.Add(service.Name, service);
            read.Add(service);
        }

        return read.ToImmutable();
    }

    // a name as an Implements statement writes it may be qualified with its project's name (RDWeb.IController): its last part is the type's own name.
    private static bool IsNamed(string written, string name)
        => string.Equals(written, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(written[(written.LastIndexOf('.') + 1)..], name, StringComparison.OrdinalIgnoreCase);

    private static RDWebServiceException Refused(string format, params object[] arguments)
        => new(string.Format(CultureInfo.CurrentUICulture, format, arguments));
}
