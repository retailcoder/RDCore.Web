using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace RDCore.Web.Machines;

/// <summary>
/// A call of a procedure of an RD-VBA program, as a request makes it.
/// </summary>
/// <param name="ModuleName">The name of the module that declares the procedure.</param>
/// <param name="ProcedureName">The name of the procedure.</param>
/// <param name="Arguments">
/// The arguments the request binds, by parameter name, compared without regard to case. A parameter the request binds no argument to is left out, and the
/// procedure gets no argument for it, as a call that omits an <c>Optional</c> argument does. A value from the route or the query string is a JSON string;
/// a request body is the JSON it holds, a <see langword="null"/> node for JSON <c>null</c>.
/// </param>
public sealed record class RDWebInvocation(string ModuleName, string ProcedureName, ImmutableDictionary<string, JsonNode?> Arguments);
