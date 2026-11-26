using RDCore.Web.Machines;
using System.Collections.Concurrent;

namespace RDCore.Web.Tests;

// a machine whose answers the test writes: what a session of an RD-VBA program would answer, without the program. It keeps each invocation it was given.
internal sealed class ScriptedMachine(Func<RDWebInvocation, CancellationToken, Task<RDWebInvocationResult>> answer) : IRDWebMachine
{
    public ScriptedMachine(Func<RDWebInvocation, RDWebInvocationResult> answer)
        : this((invocation, _) => Task.FromResult(answer(invocation)))
    {
    }

    public ConcurrentQueue<RDWebInvocation> Invocations { get; } = new();

    public Task<RDWebInvocationResult> InvokeAsync(RDWebInvocation invocation, CancellationToken cancellationToken)
    {
        Invocations.Enqueue(invocation);
        return answer(invocation, cancellationToken);
    }
}
