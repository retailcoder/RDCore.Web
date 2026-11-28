using RDCore.SDK.Runtime.Hosting;
using RDCore.SDK.Workspace;
using System.Collections.Concurrent;

namespace RDCore.Web.Tests;

// a program of the test's documents, none unless it gives some, whose machines the test makes: the machines it made, in the order it made them.
internal sealed class ScriptedProgram(Func<ScriptedMachine> make, IReadOnlyList<RDCoreDocument>? documents = null) : IComposedProgram
{
    public ConcurrentQueue<ScriptedMachine> Made { get; } = new();

    public ScriptedMachine this[int index] => Made.ElementAt(index);

    public IReadOnlyList<RDCoreDocument> Documents { get; } = documents ?? [];

    public ValueTask<IProgramMachine> CreateMachineAsync(CancellationToken cancellationToken)
    {
        var machine = make();
        Made.Enqueue(machine);
        return ValueTask.FromResult<IProgramMachine>(machine);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
