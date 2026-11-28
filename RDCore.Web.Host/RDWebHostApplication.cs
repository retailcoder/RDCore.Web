using RDCore.Runtime.Hosting;
using RDCore.SDK.Runtime.Hosting;
using RDCore.Web.Endpoints;
using RDCore.Web.Hosting;
using RDCore.Web.Machines;
using System.Collections.Immutable;

namespace RDCore.Web.Host;

/// <summary>
/// The ASP.NET Core application the host runs, and the program it serves when its configuration names a composition: the program's machines, and the
/// endpoints its controller modules declare.
/// </summary>
public sealed class RDWebHostApplication : IAsyncDisposable
{
    /// <summary>
    /// The configuration key of the composition the host serves, the file <c>rdc compose</c> wrote: <c>--RDWeb:Composition &lt;file&gt;</c> on the command
    /// line. A relative path is relative to the current directory.
    /// </summary>
    public const string CompositionKey = "RDWeb:Composition";

    /// <summary>
    /// The configuration section of the machine pool's options (<see cref="RDWebMachinePoolOptions"/>), such as <c>RDWeb:Pool:MaxMachines</c>.
    /// </summary>
    public const string PoolSection = "RDWeb:Pool";

    /// <summary>
    /// The configuration section of the kept machines' options (<see cref="RDWebKeptMachinesOptions"/>), such as <c>RDWeb:KeptMachines:IdleTimeout</c>.
    /// </summary>
    public const string KeptMachinesSection = "RDWeb:KeptMachines";

    private readonly IComposedProgram? _program;
    private readonly RDWebMachinePool? _pool;
    private readonly RDWebKeptMachines? _kept;

    private RDWebHostApplication(WebApplication application, IComposedProgram? program, RDWebMachinePool? pool, RDWebKeptMachines? kept,
        ImmutableArray<RDWebEndpoint> endpoints)
    {
        Application = application;
        _program = program;
        _pool = pool;
        _kept = kept;
        Endpoints = endpoints;
    }

    /// <summary>
    /// The application, built and not started.
    /// </summary>
    public WebApplication Application { get; }

    /// <summary>
    /// The endpoints the application serves: none when its configuration names no composition.
    /// </summary>
    public ImmutableArray<RDWebEndpoint> Endpoints { get; }

    /// <summary>
    /// Builds the application. It serves the files of its web root (<c>wwwroot</c>, or <c>--webroot &lt;folder&gt;</c>), <c>index.html</c> at the root.
    /// When its configuration names a composition (<see cref="CompositionKey"/>), it loads the program, is the external host of its machines
    /// (<see cref="ProgramExternalHost"/>), starts the machine pool and the kept machines with the options of their sections, and maps the endpoints of
    /// the program's controller modules.
    /// </summary>
    /// <param name="options">The options of the application, its command-line arguments and content root among them.</param>
    /// <param name="cancellationToken">Cancels loading the program and starting its pool.</param>
    /// <returns>The application, with what it serves; disposing it disposes the program and its machines.</returns>
    /// <exception cref="RDWebEndpointException">A controller module declares an endpoint that cannot be served.</exception>
    public static Task<RDWebHostApplication> CreateAsync(WebApplicationOptions options, CancellationToken cancellationToken)
        => CreateAsync(options, ComposedProgramFile.LoadAsync, cancellationToken);

    // the loader is the tests' when they give one: a program of their own, which no composition file holds.
    internal static async Task<RDWebHostApplication> CreateAsync(WebApplicationOptions options,
        Func<string, ComposedProgramOptions, CancellationToken, Task<IComposedProgram>> load, CancellationToken cancellationToken)
    {
        var app = WebApplication.CreateBuilder(options).Build();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        if (app.Configuration[CompositionKey] is not { Length: > 0 } composition)
        {
            return new RDWebHostApplication(app, null, null, null, []);
        }

        IComposedProgram? program = null;
        RDWebMachinePool? pool = null;
        try
        {
            program = await load(Path.GetFullPath(composition),
                new ComposedProgramOptions { ExternalHost = ProgramExternalHost.Executable, Loggers = app.Services.GetService<ILoggerFactory>() },
                cancellationToken);
            var endpoints = RDWebEndpointReader.Read(program);
            pool = await RDWebMachinePool.StartAsync(program, app.Configuration.GetSection(PoolSection).Get<RDWebMachinePoolOptions>() ?? new(),
                cancellationToken);
            var kept = new RDWebKeptMachines(program, app.Configuration.GetSection(KeptMachinesSection).Get<RDWebKeptMachinesOptions>() ?? new());
            app.MapRDWebEndpoints(endpoints, pool, kept);
            return new RDWebHostApplication(app, program, pool, kept, endpoints);
        }
        catch
        {
            if (pool is not null)
            {
                await pool.DisposeAsync();
            }

            if (program is not null)
            {
                await program.DisposeAsync();
            }

            await app.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Disposes the application, then the kept machines and the pool, then the program.
    /// </summary>
    /// <returns>A task that completes once everything is disposed.</returns>
    public async ValueTask DisposeAsync()
    {
        await Application.DisposeAsync();
        if (_kept is not null)
        {
            await _kept.DisposeAsync();
        }

        if (_pool is not null)
        {
            await _pool.DisposeAsync();
        }

        if (_program is not null)
        {
            await _program.DisposeAsync();
        }
    }
}
