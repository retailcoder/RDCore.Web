using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using RDCore.Web.Host;
using RDCore.Web.Tests.Hosting;
using System.Net;

namespace RDCore.Web.Tests.Host;

/// <summary>
/// The host's application (<see cref="RDWebHostApplication"/>): it serves its <c>wwwroot</c> folder, the page that calls an endpoint at its root.
/// </summary>
[TestClass]
public sealed class RDWebHostApplicationTests
{
    public TestContext TestContext { get; set; } = null!;

    private Task<RunningApplication> StartAsync()
    {
        var app = RDWebHostApplication.Create(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory, EnvironmentName = "Production" });
        app.Urls.Add("http://127.0.0.1:0");
        return RunningApplication.StartAsync(app, _ => { }, TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task TheRoot_ServesThePage_WhichLoadsItsScript()
    {
        await using var app = await StartAsync();

        using var response = await app.Client.GetAsync("/", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(TestContext.CancellationToken), "<script src=\"app.js\">");
    }

    [TestMethod]
    public async Task ThePagesScript_IsServed()
    {
        await using var app = await StartAsync();

        using var response = await app.Client.GetAsync("app.js", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/javascript", response.Content.Headers.ContentType?.MediaType);
    }
}
