namespace RDCore.Web.Host;

/// <summary>
/// The ASP.NET Core application the host runs.
/// </summary>
public static class RDWebHostApplication
{
    /// <summary>
    /// The application: it serves the files of its <c>wwwroot</c> folder, <c>index.html</c> at the root.
    /// </summary>
    /// <param name="options">The options of the application, its command-line arguments and content root among them.</param>
    /// <returns>The application, built and not started.</returns>
    public static WebApplication Create(WebApplicationOptions options)
    {
        var app = WebApplication.CreateBuilder(options).Build();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        return app;
    }
}
