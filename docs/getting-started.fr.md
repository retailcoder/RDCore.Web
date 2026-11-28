# Pour démarrer
<sup>_This document is available in [English](getting-started.md)_</sup>

Cette page construit et lance l'hôte de RDCore.Web, écrit un module contrôleur, le compose en un programme et l'appelle. Elle suppose une ligne de
commande, [Git](https://git-scm.com/), le [SDK .NET 11](https://dotnet.microsoft.com/download/dotnet/11.0) et `rdc`, la ligne de commande de RDCore,
compilée à partir du référentiel [RDCore](https://github.com/rubberduck-vba/RDCore). Elle ne suppose aucune connaissance d'ASP.NET Core :
l'[aperçu](overview.fr.md) explique comment RDCore.Web s'en sert.

---
## 1. Construire et tester

```powershell
git clone https://github.com/rubberduck-vba/RDCore.Web.git
cd RDCore.Web
dotnet build RDCore.Web.slnx
dotnet test RDCore.Web.slnx
```

|Projet|Ce que c'est|
|---|---|
|`RDCore.Web`|La bibliothèque du cadriciel.|
|`RDCore.Web.Host`|L'application ASP.NET Core qui sert les points de terminaison et les pages d'une application.|
|`RDCore.Web.Tests`|Les tests.|

---
## 2. Lancer l'hôte

```powershell
dotnet run --project RDCore.Web.Host
```

L'hôte écrit l'adresse où il écoute, `http://localhost:5000` par défaut;
[Kestrel](https://learn.microsoft.com/fr-fr/aspnet/core/fundamentals/servers/kestrel) explique comment la changer. Ouvrez cette adresse dans un navigateur :
l'hôte sert `index.html` depuis son dossier `wwwroot`, une page avec une case **Name** et un bouton **Call**. Sans programme, l'hôte ne sert que sa page.
Ctrl+C arrête l'hôte.

`RDWebHostApplicationTests.TheRoot_ServesThePage_WhichLoadsItsScript` fixe la page.

---
## 3. Écrire un module contrôleur

La page appelle ce point de terminaison, `GET /api/hello/{name}`. Dans un nouveau dossier, `Hello`, enregistrez ce module de classe sous `Greetings.cls` :

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

|Ligne|Ce qu'elle déclare|
|---|---|
|`'@Controller("api")`|Le module de classe est un contrôleur. Sa route, `api`, préfixe les routes de ses points de terminaison. Elle va dans la section des déclarations du module.|
|`'@HttpGet("hello/{name}")`|`Hello` sert les requêtes `GET` sur `api/hello/{name}`. Elle va sur la ligne au-dessus de la procédure.|
|`{name}`|Un paramètre de route : il lie le paramètre `name` de la procédure, comme chaîne.|

Un paramètre de route qui ne nomme aucun paramètre de la procédure est une erreur, tout comme une classe contrôleur dont `VB_PredeclaredId` est `True`.
La [référence de RDCore.Web](specs/rdweb.md), en anglais, donne les règles de chaque annotation.

`RDWebEndpointTests.AnHttpGetAnnotation_ServesTheProcedureForGet_OnTheControllersRouteAndItsOwn` et
`RDWebEndpointsHostingTests.TheRouteValues_BindTheirParameters_AsStrings` fixent le point de terminaison et sa liaison.

---
## 4. Composer le programme

À partir du dossier `Hello` :

```powershell
rdc new .
rdc compose .
```

`rdc new` écrit le fichier du projet, `.rdproj`, avec les modules du dossier. `rdc compose` écrit le programme dans `bin/Debug/Hello.rdcomposition`.

---
## 5. Lancer l'hôte avec le programme

À partir du dossier `RDCore.Web`, avec le chemin complet de la composition :

```powershell
dotnet run --project RDCore.Web.Host -- --RDWeb:Composition C:\chemin\vers\Hello\bin\Debug\Hello.rdcomposition
```

Le chemin est complet parce que `dotnet run` démarre l'hôte dans le dossier de son projet, d'où un chemin relatif serait lu. L'hôte charge le programme
et associe les points de terminaison de ses contrôleurs avant de servir une requête.
[§7.2 The RDCore.Web Host](specs/rdweb.7.0.hosting.md#72-the-rdcoreweb-host), en anglais, en donne les autres options.

`RDWebHostApplicationTests.AComposition_IsLoadedFromItsFullPath_WithTheHostAsItsMachinesExternalHost_AndItsControllersEndpointsAreServed` fixe le
chargement et les points de terminaison.

---
## 6. L'appeler

Tapez un nom dans la page et choisissez **Call**, ou envoyez la requête depuis une ligne de commande :

```powershell
curl.exe -i http://localhost:5000/api/hello/World
```

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{"greeting":"Hello, World!"}
```

Les arguments et la valeur de la procédure passent en JSON : le `Dictionary` qu'elle retourne est l'objet JSON de la réponse. La requête s'exécute sur une
machine du bassin de l'hôte, sur une nouvelle instance de `Greetings`. `RDWebEndpointsHostingTests.TheValueAProcedureReturns_IsTheJsonBodyOfTheResponse`
fixe cette réponse.

---
## Pour la suite

- [Aperçu](overview.fr.md) : comment s'agencent les parties, et leur lien avec ASP.NET Core.
- [Référence de RDCore.Web](specs/rdweb.md) : la syntaxe et les règles de chaque partie (en anglais).
- [Référence de l'API](xref:RDCore.Web.Endpoints) : les types .NET (en anglais).

---
[ACCUEIL](index.fr.md) • [HOME](index.md) | [APERÇU](overview.fr.md) • [OVERVIEW](overview.md) | DÉMARRER • [GET STARTED](getting-started.md) | [RDCore](https://rubberduck-vba.github.io/RDCore/index.fr.html) | [SDK](https://rubberduck-vba.github.io/RDCore.SDK/api/RDCore.SDK.html) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---
