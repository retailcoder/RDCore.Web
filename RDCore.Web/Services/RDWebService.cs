namespace RDCore.Web.Services;

/// <summary>
/// How long a service's object lives: what makes another one.
/// </summary>
public enum RDWebServiceLifetime
{
    /// <summary>
    /// One object per request, which each parameter that needs the service in the request's run is given.
    /// </summary>
    Scoped,

    /// <summary>
    /// One object for each parameter that needs it.
    /// </summary>
    Transient,

    /// <summary>
    /// One object per machine, kept between requests: a pool machine's reset lets go of it, and a kept machine keeps it for its life.
    /// </summary>
    Singleton,
}

/// <summary>
/// A service that a class of the program declares with <c>'@Service</c>.
/// </summary>
/// <param name="Name">
/// The service's name: the type a parameter declares to be given it. The class is that type, or names it in an <c>Implements</c> statement.
/// </param>
/// <param name="Lifetime">How long its object lives.</param>
/// <param name="ClassName">The class that makes it.</param>
/// <param name="IsPredeclared">
/// Whether the class has a default instance (<c>VB_PredeclaredId = True</c>): its object is the one its <see cref="RDWebServiceReader.DefaultFactory"/>
/// function returns, run on the default instance. Otherwise its object is a <c>New</c> one.
/// </param>
public sealed record class RDWebService(string Name, RDWebServiceLifetime Lifetime, string ClassName, bool IsPredeclared);
