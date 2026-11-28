using RDCore.Runtime.Hosting;
using RDCore.SDK.Client;
using RDCore.SDK.Extensibility;
using RDCore.SDK.Server.Configuration;
using RDCore.Web.Diagnostics;
using RDCore.Web.Host;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RDCore.Web.Tests")]

// marks the RDWeb extension a diagnostics provider: the language server sends it rdcore/diagnostics/document, and `rdc describe-ext` records it.
[assembly: ProvidesCorePlatformClientCapability<DiagnoseDocument>]

// its diagnostics are reported under RDW codes, and each code has a page under RDCore.Web's documentation.
[assembly: DiagnosticPrefix(RDWebDiagnostics.Prefix, HelpBaseUri = RDWebDiagnostics.HelpBaseUri)]

// started as the external host of its own machines, it is that and nothing else, on the process's main thread: before the first await.
if (ProgramExternalHost.IsThisProcess)
{
    return ProgramExternalHost.Run(args);
}

// started by the platform, with the pipe it names, the host is the RDWeb extension; started otherwise, it serves the application.
if (SdkCommandLine.IsServerLaunch(args))
{
    using var extension = new RDWebExtensionHost();
    return await extension.RunAsync(args);
}

await using var host = await RDWebHostApplication.CreateAsync(new WebApplicationOptions { Args = args }, CancellationToken.None);
await host.Application.RunAsync();
return 0;
