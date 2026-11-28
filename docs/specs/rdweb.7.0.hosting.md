# 7.0 Hosting

An RDCore.Web application is an ASP.NET Core application: it maps the endpoints of its controller modules into its routing, and a pool of machines serves
them ([ASP.NET Core fundamentals](https://learn.microsoft.com/aspnet/core/fundamentals/)).

---
## 7.1 Mapping Endpoints

### Syntax

```csharp
// program: the application's composed program, such as the composition `rdc compose` wrote (ComposedProgramFile.LoadAsync, RDCore.Runtime.Hosting).
var endpoints = RDWebEndpointReader.Read(program);
await using var pool = await RDWebMachinePool.StartAsync(program, new RDWebMachinePoolOptions(), cancellationToken);
app.MapRDWebEndpoints(endpoints, pool);
```

`MapRDWebEndpoints` returns a builder of the conventions of every endpoint it mapped, such as an authorization policy
([Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)).

### Runtime Semantics

Each endpoint is mapped to its route ([§3.0](rdweb.3.0.routes.md)) and its HTTP method. Its display name is the method, the route and the procedure, such as
`GET api/orders/{id} (OrdersController.Find)`.

A request to an endpoint:

1. binds the procedure's arguments from its route values, query string and body ([§4.0](rdweb.4.0.binding.md));
2. runs the procedure on a machine of the pool ([§6.1](rdweb.6.0.machines.md#61-the-machine-pool));
3. answers with how the procedure ended ([§5.0](rdweb.5.0.responses.md)).

A request to an endpoint's route with another HTTP method answers `405 Method Not Allowed`, and runs nothing.

---
## 7.2 The RDCore.Web Host

### Runtime Semantics

`RDCore.Web.Host` runs one of three ways.

|Started|It is|
|---|---|
|By the RDCore platform, with the pipe the platform names|The RDWeb extension: it reports the `RDW` diagnostics of the documents the language server sends it, each linked to its page ([Diagnostics](../diagnostics/rdw00001.md)).|
|By itself, as the external host of its machines|The process that makes their forms, the objects of automation servers and their native calls. A deployed host needs no `rdc` beside it.|
|Any other way|An ASP.NET Core application that serves the files of its web root, `index.html` at the root, and the program its configuration names. It reads its command-line arguments and `appsettings.json` as ASP.NET Core does.|

The application's configuration:

|Key|Meaning|
|---|---|
|`RDWeb:Composition`|The program the host serves: the composition file that `rdc compose` wrote. A relative path is relative to the current directory. Without it, the host serves its static files only.|
|`RDWeb:Pool`|The options of the machine pool ([§6.1](rdweb.6.0.machines.md#61-the-machine-pool)), such as `RDWeb:Pool:MaxMachines`.|
|`RDWeb:KeptMachines`|The options of the kept machines ([§6.2](rdweb.6.0.machines.md#62-kept-machines)), such as `RDWeb:KeptMachines:IdleTimeout`.|
|`webroot`|The folder of the static files: `wwwroot` by default.|

Given a composition, the host loads the program and maps the endpoints of its controller modules ([§7.1](#71-mapping-endpoints)) before it serves any
request. Its pool makes its `MinMachines` machines first. A controller module that declares an endpoint that cannot be served stops the host before it
starts ([§2.0](rdweb.2.0.annotations.md)).

```
RDCore.Web.Host --RDWeb:Composition bin/Debug/NavalBattle.rdcomposition --webroot web/dist --urls http://localhost:5000
```

---
## Implementation

|Type or member|Role|
|---|---|
|[`MapRDWebEndpoints`](xref:RDCore.Web.Hosting.RDWebEndpointRouteBuilderExtensions.MapRDWebEndpoints*)|Maps each endpoint to its route and HTTP method, served by the pool.|
|[`RDWebMachinePool.StartAsync`](xref:RDCore.Web.Machines.RDWebMachinePool.StartAsync*)|Starts the pool the endpoints are served by.|
|[`RDWebEndpointReader`](xref:RDCore.Web.Endpoints.RDWebEndpointReader)|Reads the endpoints a module's annotations declare, or those of every module of a program.|
|`RDWebHostApplication`|The host's application, and the program it serves.|

---
## Tests

|Rule|Test|
|---|---|
|An endpoint serves only its own HTTP method|`RDWebEndpointsHostingTests.AnEndpoint_ServesOnlyItsOwnMethod`|
|Through a pool of one machine, every request is served|`RDWebEndpointsHostingTests.ThroughAPoolOfOneMachine_EveryRequestIsServed`|
|The host serves its page at the root|`RDWebHostApplicationTests.TheRoot_ServesThePage_WhichLoadsItsScript`|
|The host serves the page's script|`RDWebHostApplicationTests.ThePagesScript_IsServed`|
|Without a composition, the host loads no program and serves no endpoint|`RDWebHostApplicationTests.WithoutAComposition_TheHostLoadsNoProgram_AndServesNoEndpoint`|
|A composition is loaded from its full path, with the host as its machines' external host, and its endpoints are served|`RDWebHostApplicationTests.AComposition_IsLoadedFromItsFullPath_WithTheHostAsItsMachinesExternalHost_AndItsControllersEndpointsAreServed`|
|The pool's options are read from `RDWeb:Pool`|`RDWebHostApplicationTests.ThePoolsOptions_AreReadFromTheirSection`|
|A kept machine's endpoint is served by the kept machines, with the options of `RDWeb:KeptMachines`|`RDWebHostApplicationTests.AKeptMachinesEndpoint_IsServedByTheKeptMachines_WithTheOptionsOfTheirSection`|
|A program's endpoints are those of every module of its documents|`RDWebEndpointReaderTests.AProgramsEndpoints_AreThoseOfEveryModuleOfItsDocuments_BlocksIncluded_EachNamedAsItsDocumentNamesIt`|
|The extension reports the RDW findings, each linked to its page|`RDWebExtensionTests.TheExtensionsHandler_ReportsRDWebsFindings_EachLinkedToItsPage`|
