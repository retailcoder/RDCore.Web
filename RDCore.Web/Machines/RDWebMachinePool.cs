using System.Threading.Channels;

namespace RDCore.Web.Machines;

/// <summary>
/// The machines of an RD-VBA program: each request borrows an idle one, runs on it alone, and returns it. A request that finds none idle waits for one,
/// without holding a thread.
/// </summary>
/// <remarks>
/// A pool of one machine serves its requests one at a time, in the order they arrive.
/// </remarks>
public sealed class RDWebMachinePool : IRDWebMachine
{
    private readonly Channel<IRDWebMachine> _idle = Channel.CreateUnbounded<IRDWebMachine>();

    /// <summary>
    /// A pool of the specified machines, all of them idle.
    /// </summary>
    /// <param name="machines">The machines: at least one, each a session of its own.</param>
    /// <exception cref="ArgumentException"><paramref name="machines"/> is empty.</exception>
    public RDWebMachinePool(IEnumerable<IRDWebMachine> machines)
    {
        foreach (var machine in machines)
        {
            _idle.Writer.TryWrite(machine);
            Size++;
        }

        if (Size == 0)
        {
            throw new ArgumentException(RDWebResources.Pool_NoMachine, nameof(machines));
        }
    }

    /// <summary>
    /// How many machines the pool holds.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Runs the invocation on an idle machine, once there is one, and returns the machine to the pool when it ends.
    /// </summary>
    /// <param name="invocation">The procedure, and its arguments.</param>
    /// <param name="cancellationToken">Cancels the wait for a machine, and the invocation.</param>
    /// <returns>What the machine answered.</returns>
    public async Task<RDWebInvocationResult> InvokeAsync(RDWebInvocation invocation, CancellationToken cancellationToken)
    {
        var machine = await _idle.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await machine.InvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _idle.Writer.TryWrite(machine);
        }
    }
}
