using Microsoft.AspNetCore.Builder;
using RDCore.Runtime.Hosting;
using RDCore.SDK.Platform.Protocol;
using RDCore.SDK.Runtime.Hosting;
using RDCore.SDK.Workspace;
using RDCore.Web.Endpoints;
using RDCore.Web.Host;
using RDCore.Web.Tests.Hosting;
using System.Net;
using System.Text.Json.Nodes;

namespace RDCore.Web.Tests.Host;

/// <summary>
/// The host's application (<see cref="RDWebHostApplication"/>): it serves its <c>wwwroot</c> folder, the page that calls an endpoint at its root, and, given a
/// composition, the endpoints of the program's controller modules. The program is the test's: the loader it gives returns it.
/// </summary>
[TestClass]
public sealed class RDWebHostApplicationTests
{
    private static readonly Uri Greetings = new("file:///c:/app/Greetings.bas");

    private static readonly Uri Games = new("file:///c:/app/GamesController.cls");

    public TestContext TestContext { get; set; } = null!;

    // the host built with the arguments given, its program loaded by the test's loader; its application is started by the test.
    private Task<RDWebHostApplication> CreateAsync(Func<string, ComposedProgramOptions, CancellationToken, Task<IComposedProgram>> load,
        params string[] args)
        => RDWebHostApplication.CreateAsync(
            new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory, EnvironmentName = "Production" }, load,
            TestContext.CancellationToken);

    private static Func<string, ComposedProgramOptions, CancellationToken, Task<IComposedProgram>> Returns(IComposedProgram program)
        => (_, _, _) => Task.FromResult(program);

    private static readonly Func<string, ComposedProgramOptions, CancellationToken, Task<IComposedProgram>> NoLoad
        = (_, _, _) => throw new AssertFailedException("no composition is given");

    private Task<RunningApplication> StartAsync(RDWebHostApplication host)
    {
        host.Application.Urls.Add("http://127.0.0.1:0");
        return RunningApplication.StartAsync(host.Application, _ => { }, TestContext.CancellationToken);
    }

    private static ScriptedProgram Program(Func<ProgramInvocation, ProgramInvocationResult> answer, params RDCoreDocument[] documents)
        => new(() => new ScriptedMachine(answer), documents);

    [TestMethod]
    public async Task TheRoot_ServesThePage_WhichLoadsItsScript()
    {
        await using var host = await CreateAsync(NoLoad);
        await using var app = await StartAsync(host);

        using var response = await app.Client.GetAsync("/", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(TestContext.CancellationToken), "<script src=\"app.js\">");
    }

    [TestMethod]
    public async Task ThePagesScript_IsServed()
    {
        await using var host = await CreateAsync(NoLoad);
        await using var app = await StartAsync(host);

        using var response = await app.Client.GetAsync("app.js", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/javascript", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task WithoutAComposition_TheHostLoadsNoProgram_AndServesNoEndpoint()
    {
        await using var host = await CreateAsync(NoLoad);

        Assert.IsEmpty(host.Endpoints);
    }

    [TestMethod]
    public async Task AComposition_IsLoadedFromItsFullPath_WithTheHostAsItsMachinesExternalHost_AndItsControllersEndpointsAreServed()
    {
        string? loadedPath = null;
        ComposedProgramOptions? loadedWith = null;
        var program = Program(_ => ProgramInvocationResult.Returned(JsonValue.Create("Hello!")),
            ControllerDocument.Of(Greetings, "api", null, new ControllerDocument.Action("Hello", RDWebAnnotations.HttpGet, "hello")));
        await using var host = await CreateAsync((path, options, _) =>
        {
            (loadedPath, loadedWith) = (path, options);
            return Task.FromResult<IComposedProgram>(program);
        }, "--RDWeb:Composition", "NavalBattle.rdcomposition");
        await using var app = await StartAsync(host);

        using var response = await app.Client.GetAsync("api/hello", TestContext.CancellationToken);

        Assert.AreEqual(Path.GetFullPath("NavalBattle.rdcomposition"), loadedPath);
        Assert.AreEqual(ProgramExternalHost.Executable, loadedWith?.ExternalHost);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("\"Hello!\"", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        var invocation = program.Made.Single().Invocations.Single();
        Assert.AreEqual("Greetings.Hello", $"{invocation.Module}.{invocation.Procedure}");
    }

    [TestMethod]
    public async Task ThePoolsOptions_AreReadFromTheirSection()
    {
        var program = Program(_ => ProgramInvocationResult.Completed());

        await using var host = await CreateAsync(Returns(program),
            "--RDWeb:Composition", "app.rdcomposition", "--RDWeb:Pool:MinMachines", "2", "--RDWeb:Pool:MaxMachines", "2");

        Assert.HasCount(2, program.Made, "the pool starts with MinMachines machines; its default is 1");
    }

    [TestMethod]
    public async Task AKeptMachinesEndpoint_IsServedByTheKeptMachines_WithTheOptionsOfTheirSection()
    {
        var program = Program(_ => ProgramInvocationResult.Completed(),
            ControllerDocument.Of(Games, "api/games/{game}", "game",
                new ControllerDocument.Action("NewGame", RDWebAnnotations.HttpPost, "new", NewMachine: true)));
        await using var host = await CreateAsync(Returns(program),
            "--RDWeb:Composition", "app.rdcomposition", "--RDWeb:Pool:MinMachines", "0", "--RDWeb:KeptMachines:MaxMachines", "1");
        await using var app = await StartAsync(host);

        using var first = await app.Client.PostAsync("api/games/g1/new", null, TestContext.CancellationToken);
        using var second = await app.Client.PostAsync("api/games/g2/new", null, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NoContent, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, second.StatusCode, "one kept machine at most: the second game finds none");
        Assert.HasCount(1, program.Made, "the pool made none, and the kept machines one");
    }
}
