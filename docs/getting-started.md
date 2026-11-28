# Getting Started
<sup>_Ce document est disponible en [français](getting-started.fr.md)_</sup>

This page builds and runs the RDCore.Web host, writes a controller module, composes it into a program, and calls it. It assumes a command line,
[Git](https://git-scm.com/), the [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0), and `rdc`, the RDCore command line, built from the
[RDCore](https://github.com/rubberduck-vba/RDCore) repository. It assumes no ASP.NET Core: the [Overview](overview.md) says how RDCore.Web uses it.

---
## 1. Build and test

```powershell
git clone https://github.com/rubberduck-vba/RDCore.Web.git
cd RDCore.Web
dotnet build RDCore.Web.slnx
dotnet test RDCore.Web.slnx
```

|Project|What it is|
|---|---|
|`RDCore.Web`|The framework library.|
|`RDCore.Web.Host`|The ASP.NET Core application that serves an application's endpoints and pages.|
|`RDCore.Web.Tests`|The tests.|

---
## 2. Run the host

```powershell
dotnet run --project RDCore.Web.Host
```

The host writes the address it listens on, `http://localhost:5000` by default; [Kestrel](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel)
explains how to change it. Open that address in a browser: the host serves `index.html` from its `wwwroot` folder, a page with a **Name** box and a
**Call** button. Without a program, the host serves its page only. Press Ctrl+C to stop the host.

`RDWebHostApplicationTests.TheRoot_ServesThePage_WhichLoadsItsScript` pins the page.

---
## 3. Write a controller module

The page calls this endpoint, `GET /api/hello/{name}`. In a new folder, `Hello`, save this class module as `Greetings.cls`:

```vb
VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Greetings"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
'@Controller("api")
Option Explicit

'@HttpGet("hello/{name}")
Public Function Hello(ByVal name As Variant) As Variant
    Dim greeting As Object
    Set greeting = CreateObject("Scripting.Dictionary")
    greeting.Add "greeting", "Hello, " & name & "!"
    Set Hello = greeting
End Function
```

|Line|What it declares|
|---|---|
|`'@Controller("api")`|The class module is a controller. Its route, `api`, prefixes the routes of its endpoints. It goes in the module's declarations section.|
|`'@HttpGet("hello/{name}")`|`Hello` serves `GET` requests on `api/hello/{name}`. It goes on the line above the procedure.|
|`{name}`|A route parameter: it binds the procedure's `name` parameter, as a string.|

A route parameter that names no parameter of the procedure is an error, and so is a controller class whose `VB_PredeclaredId` is `True`. The
[RDCore.Web Reference](specs/rdweb.md) has the rules of each annotation.

`RDWebEndpointTests.AnHttpGetAnnotation_ServesTheProcedureForGet_OnTheControllersRouteAndItsOwn` and
`RDWebEndpointsHostingTests.TheRouteValues_BindTheirParameters_AsStrings` pin the endpoint and its binding.

---
## 4. Compose the program

From the `Hello` folder:

```powershell
rdc new .
rdc compose .
```

`rdc new` writes the project file, `.rdproj`, with the folder's modules. `rdc compose` writes the program to `bin/Debug/Hello.rdcomposition`.

---
## 5. Run the host with the program

From the `RDCore.Web` folder, with the full path of the composition:

```powershell
dotnet run --project RDCore.Web.Host -- --RDWeb:Composition C:\path\to\Hello\bin\Debug\Hello.rdcomposition
```

The path is a full one because `dotnet run` starts the host in its project folder, and a relative path is read from there. The host loads the program and
maps the endpoints of its controllers before it serves any request. [§7.2 The RDCore.Web Host](specs/rdweb.7.0.hosting.md#72-the-rdcoreweb-host) lists
its other options.

`RDWebHostApplicationTests.AComposition_IsLoadedFromItsFullPath_WithTheHostAsItsMachinesExternalHost_AndItsControllersEndpointsAreServed` pins the
loading and the endpoints.

---
## 6. Call it

Type a name on the page and select **Call**, or send the request from a command line:

```powershell
curl.exe -i http://localhost:5000/api/hello/World
```

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{"greeting":"Hello, World!"}
```

The procedure's arguments and value pass as JSON: the `Dictionary` it returns is the response's JSON object. The request runs on a machine of the host's
pool, on a new instance of `Greetings`. `RDWebEndpointsHostingTests.TheValueAProcedureReturns_IsTheJsonBodyOfTheResponse` pins that response.

---
## Next

- [Overview](overview.md): how the parts fit together, and how they relate to ASP.NET Core.
- [RDCore.Web Reference](specs/rdweb.md): each part's syntax and rules (English only).
- [API reference](xref:RDCore.Web.Endpoints): the .NET types (English only).

---
[ACCUEIL](index.fr.md) • [HOME](index.md) | [APERÇU](overview.fr.md) • [OVERVIEW](overview.md) | [DÉMARRER](getting-started.fr.md) • GET STARTED | [RDCore](https://rubberduck-vba.github.io/RDCore/) | [SDK](https://rubberduck-vba.github.io/RDCore.SDK/api/RDCore.SDK.html) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---
