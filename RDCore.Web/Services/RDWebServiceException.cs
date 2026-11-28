namespace RDCore.Web.Services;

/// <summary>
/// A class's <c>'@Service</c> declares a service that cannot be made, or two classes declare one service name (<c>RDW00005</c>): the host serves nothing.
/// </summary>
public sealed class RDWebServiceException : Exception
{
    /// <summary>
    /// A class's <c>'@Service</c> declares a service that cannot be made, or two classes declare one service name.
    /// </summary>
    /// <param name="message">What is wrong with the service, naming its class, in the language of the user interface.</param>
    public RDWebServiceException(string message)
        : base(message)
    {
    }
}
