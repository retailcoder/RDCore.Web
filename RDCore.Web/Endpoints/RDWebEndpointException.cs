namespace RDCore.Web.Endpoints;

/// <summary>
/// The annotations of a controller module declare an endpoint that cannot be served: its route is not a valid route template, or it names a route parameter
/// that is not a parameter of the procedure.
/// </summary>
public sealed class RDWebEndpointException : Exception
{
    /// <summary>
    /// The annotations of a controller module declare an endpoint that cannot be served.
    /// </summary>
    /// <param name="message">What is wrong with the endpoint, in the language of the user interface.</param>
    /// <param name="innerException">What the route template's parser raised, when it is the route that is wrong.</param>
    public RDWebEndpointException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
