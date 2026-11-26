using RDCore.SDK.Platform.Protocol;
using System.Text.Json.Nodes;

namespace RDCore.Web.Machines;

/// <summary>
/// How an invocation of a procedure ended: the value it returned, or why it did not complete.
/// </summary>
public sealed record class RDWebInvocationResult
{
    private RDWebInvocationResult(ExecutionOutcome outcome)
    {
        Outcome = outcome;
    }

    /// <summary>
    /// How the invocation ended.
    /// </summary>
    public ExecutionOutcome Outcome { get; }

    /// <summary>
    /// Whether the procedure returned a value: a <c>Function</c> or a <c>Property Get</c> that completed. A <c>Sub</c> returns none.
    /// </summary>
    public bool HasValue { get; private init; }

    /// <summary>
    /// The value the procedure returned, as JSON; a <see langword="null"/> node for <c>Null</c> and <c>Empty</c>, and when <see cref="HasValue"/> is
    /// <see langword="false"/>.
    /// </summary>
    public JsonNode? Value { get; private init; }

    /// <summary>
    /// The number of the run-time error, when <see cref="Outcome"/> is <see cref="ExecutionOutcome.RuntimeError"/>; 0 otherwise.
    /// </summary>
    public int ErrorNumber { get; private init; }

    /// <summary>
    /// The description of the run-time error, or a short explanation of any other outcome that is not <see cref="ExecutionOutcome.Completed"/>.
    /// </summary>
    public string ErrorMessage { get; private init; } = string.Empty;

    /// <summary>
    /// The RD-VBAL §2.6.3 diagnostic code of the run-time error (<c>VBR</c> for one the runtime raised, <c>VBA</c> for one the program raised); empty for any
    /// other outcome.
    /// </summary>
    public string ErrorCode { get; private init; } = string.Empty;

    /// <summary>
    /// A procedure that completed and returned a value.
    /// </summary>
    /// <param name="value">The value, as JSON; <see langword="null"/> for <c>Null</c> and <c>Empty</c>.</param>
    /// <returns>A <see cref="ExecutionOutcome.Completed"/> result with a value.</returns>
    public static RDWebInvocationResult Returned(JsonNode? value) => new(ExecutionOutcome.Completed) { HasValue = true, Value = value };

    /// <summary>
    /// A procedure that completed and returned no value: a <c>Sub</c>.
    /// </summary>
    /// <returns>A <see cref="ExecutionOutcome.Completed"/> result with no value.</returns>
    public static RDWebInvocationResult Completed() => new(ExecutionOutcome.Completed);

    /// <summary>
    /// A procedure that raised a run-time error that nothing handled.
    /// </summary>
    /// <param name="number">The error's number, as <c>Err.Number</c> reports it.</param>
    /// <param name="description">The error's description, as <c>Err.Description</c> reports it.</param>
    /// <param name="code">The error's RD-VBAL §2.6.3 diagnostic code.</param>
    /// <returns>A <see cref="ExecutionOutcome.RuntimeError"/> result.</returns>
    public static RDWebInvocationResult RuntimeError(int number, string description, string code)
        => new(ExecutionOutcome.RuntimeError) { ErrorNumber = number, ErrorMessage = description, ErrorCode = code };

    /// <summary>
    /// An invocation that ended otherwise: the procedure was not found, the program ended or was interrupted, or the machine refused it.
    /// </summary>
    /// <param name="outcome">How it ended: neither <see cref="ExecutionOutcome.Completed"/> nor <see cref="ExecutionOutcome.RuntimeError"/>.</param>
    /// <param name="explanation">A short explanation of the outcome.</param>
    /// <returns>A result of that outcome.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="outcome"/> is <see cref="ExecutionOutcome.Completed"/> or <see cref="ExecutionOutcome.RuntimeError"/>.
    /// </exception>
    public static RDWebInvocationResult Ended(ExecutionOutcome outcome, string explanation)
        => outcome is ExecutionOutcome.Completed or ExecutionOutcome.RuntimeError
            ? throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
            : new(outcome) { ErrorMessage = explanation };
}
