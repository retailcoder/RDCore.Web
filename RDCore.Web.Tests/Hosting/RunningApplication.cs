using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RDCore.Web.Tests.Hosting;

// an application served by Kestrel on a free port of the loopback address, and a client of it.
internal sealed class RunningApplication : IAsyncDisposable
{
    private readonly WebApplication _app;

    private RunningApplication(WebApplication app, Uri address)
    {
        _app = app;
        Client = new HttpClient { BaseAddress = address };
    }

    public HttpClient Client { get; }

    public static async Task<RunningApplication> StartAsync(Action<WebApplication> map, CancellationToken token)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        return await StartAsync(builder.Build(), map, token);
    }

    public static async Task<RunningApplication> StartAsync(WebApplication app, Action<WebApplication> map, CancellationToken token)
    {
        map(app);
        await app.StartAsync(token);
        var address = app.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.Single();
        return new RunningApplication(app, new Uri(address));
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
