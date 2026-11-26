using RDCore.Web.Machines;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace RDCore.Web.Tests.Machines;

/// <summary>
/// The pool of the machines of a program (<see cref="RDWebMachinePool"/>): one request per machine at a time, and a request that finds none idle waits for
/// one. Each machine here holds its invocation until the test releases it, so that what runs at once is set by the test, never by timing.
/// </summary>
[TestClass]
public sealed class RDWebMachinePoolTests
{
    private static readonly RDWebInvocation Invocation = new("OrdersController", "GetOrder", ImmutableDictionary<string, JsonNode?>.Empty);

    public TestContext TestContext { get; set; } = null!;

    // a machine that says when an invocation enters it, and holds it until the test releases it.
    private sealed class HeldMachine
    {
        private int _running;

        public HeldMachine()
        {
            Machine = new ScriptedMachine(async (_, token) =>
            {
                MostAtOnce = Math.Max(MostAtOnce, Interlocked.Increment(ref _running));
                Entered.Writer.TryWrite(true);
                await Release.Task.WaitAsync(token);
                Interlocked.Decrement(ref _running);
                return RDWebInvocationResult.Completed();
            });
        }

        public ScriptedMachine Machine { get; }

        public System.Threading.Channels.Channel<bool> Entered { get; } = System.Threading.Channels.Channel.CreateUnbounded<bool>();

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int MostAtOnce { get; private set; }
    }

    [TestMethod]
    public async Task APoolOfOneMachine_RunsASecondRequestOnlyOnceTheFirstHasReturned()
    {
        var held = new HeldMachine();
        var pool = new RDWebMachinePool([held.Machine]);

        var first = pool.InvokeAsync(Invocation, TestContext.CancellationToken);
        await held.Entered.Reader.ReadAsync(TestContext.CancellationToken);
        var second = pool.InvokeAsync(Invocation, TestContext.CancellationToken);

        Assert.HasCount(1, held.Machine.Invocations, "the second request waits for the machine");
        held.Release.SetResult();
        await Task.WhenAll(first, second);

        Assert.HasCount(2, held.Machine.Invocations);
        Assert.AreEqual(1, held.MostAtOnce);
    }

    [TestMethod]
    public async Task APoolOfTwoMachines_RunsTwoRequestsAtOnce_EachOnAMachineOfItsOwn()
    {
        var one = new HeldMachine();
        var two = new HeldMachine();
        var pool = new RDWebMachinePool([one.Machine, two.Machine]);

        var first = pool.InvokeAsync(Invocation, TestContext.CancellationToken);
        var second = pool.InvokeAsync(Invocation, TestContext.CancellationToken);
        await one.Entered.Reader.ReadAsync(TestContext.CancellationToken);
        await two.Entered.Reader.ReadAsync(TestContext.CancellationToken);

        one.Release.SetResult();
        two.Release.SetResult();
        await Task.WhenAll(first, second);
        Assert.AreEqual(1, one.MostAtOnce);
        Assert.AreEqual(1, two.MostAtOnce);
    }

    [TestMethod]
    public async Task ARequestCancelledWhileItWaits_DoesNotRun_AndTheMachineServesTheNextOne()
    {
        var held = new HeldMachine();
        var pool = new RDWebMachinePool([held.Machine]);
        var first = pool.InvokeAsync(Invocation, TestContext.CancellationToken);
        await held.Entered.Reader.ReadAsync(TestContext.CancellationToken);

        using var cancelled = new CancellationTokenSource();
        var waiting = pool.InvokeAsync(Invocation, cancelled.Token);
        await cancelled.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(() => waiting);

        held.Release.SetResult();
        await first;
        await pool.InvokeAsync(Invocation, TestContext.CancellationToken);
        Assert.HasCount(2, held.Machine.Invocations, "the cancelled request never reached the machine");
    }

    [TestMethod]
    public async Task AMachineWhoseInvocationThrows_GoesBackToThePool()
    {
        var calls = 0;
        var machine = new ScriptedMachine(_ => ++calls == 1 ? throw new InvalidOperationException("the session failed") : RDWebInvocationResult.Completed());
        var pool = new RDWebMachinePool([machine]);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => pool.InvokeAsync(Invocation, TestContext.CancellationToken));
        var result = await pool.InvokeAsync(Invocation, TestContext.CancellationToken);

        Assert.AreEqual(RDCore.SDK.Platform.Protocol.ExecutionOutcome.Completed, result.Outcome);
    }

    [TestMethod]
    public void APoolOfNoMachine_IsRefused()
        => Assert.ThrowsExactly<ArgumentException>(() => new RDWebMachinePool([]));
}
