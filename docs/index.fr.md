# RDCore.Web
<sup>_This document is available in [English](index.md)_</sup>

> [!NOTE]
> RDCore.Web est en pré-alpha. Son hôte sert les points de terminaison d'un programme composé : voir [Ce qui fonctionne](#ce-qui-fonctionne).

---
## Ce que c'est

**RDCore.Web** est le cadriciel web de [RDCore](https://rubberduck-vba.github.io/RDCore/index.fr.html). Il sert les procédures des modules de classe
d'un projet RD-VBA comme points de terminaison HTTP, au moyen d'ASP.NET Core. Des annotations Rubberduck en commentaire les déclarent : `'@Controller` sur
le module, `'@HttpGet` et `'@HttpPost` sur ses procédures. Les arguments et la valeur d'une procédure passent en JSON, alors l'interface peut être toute
application qui envoie des requêtes HTTP, comme une application Node.js ou React.

```vb
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

`Hello` est le point de terminaison `GET /api/hello/{name}`. Pour VBA, les annotations sont des commentaires : le module reste un module de classe VBA.

---
## Ce qui fonctionne

|Partie|État|
|---|---|
|Les points de terminaison de `'@Controller`, `'@HttpGet` et `'@HttpPost` : la méthode HTTP, et la route sous celle du contrôleur|Fonctionne : lus de l'analyse d'un module.|
|La liaison : les valeurs de la route et de la chaîne de requête aux paramètres de leur nom, et le corps d'un `POST` au paramètre que nomme `'@FromBody`|Fonctionne.|
|Les diagnostics : un `POST` qui ne nomme aucun paramètre de corps, et chaque autre paramètre que ne lient ni sa route ni son corps|Fonctionne dans une plateforme RDCore qui charge l'extension RDWeb, avec un correctif rapide qui ajoute `'@FromBody`.|
|Les réponses : la valeur de la procédure en JSON, aucune valeur, une erreur d'exécution en corps « problem details »|Fonctionne.|
|Un bassin de machines, chacune n'exécutant qu'une requête à la fois|Fonctionne.|
|Les machines gardées : une machine pour chaque clé d'un contrôleur qui a `'@KeptMachine`, créée par `'@NewMachine` et jamais réinitialisée entre ses requêtes|Fonctionne.|
|Les valeurs JSON : les objets des classes de l'espace de travail, les tableaux typés et `Collection`, et la bibliothèque `RDJson` pour le code VBA|Fonctionne, dans les arguments et la valeur d'un point de terminaison, et dans le code VBA.|
|L'hôte : le programme d'une composition (`--RDWeb:Composition`), ses points de terminaison, les fichiers de sa racine web, et l'hôte externe de ses propres machines|Fonctionne.|

---
## Pour la suite

|Page|À lire pour|
|---|---|
|[Aperçu](overview.fr.md)|Voir comment un module de classe annoté devient des points de terminaison, et le lien entre RDCore.Web, ASP.NET Core et un projet Excel ou Access.|
|[Pour démarrer](getting-started.fr.md)|Construire et lancer l'hôte, écrire un module contrôleur et l'appeler.|
|[Référence de RDCore.Web](specs/rdweb.md)|Consulter la syntaxe et les règles de chaque partie (en anglais).|
|[Référence de l'API](xref:RDCore.Web.Endpoints)|Consulter les types .NET qui les implémentent (en anglais).|
|[Dépôt RDCore.Web](https://github.com/rubberduck-vba/RDCore.Web)|Lire le code source, son état et la façon de le construire.|

---
ACCUEIL • [HOME](index.md) | [APERÇU](overview.fr.md) • [OVERVIEW](overview.md) | [DÉMARRER](getting-started.fr.md) • [GET STARTED](getting-started.md) | [RDCore](https://rubberduck-vba.github.io/RDCore/index.fr.html) | [SDK](https://rubberduck-vba.github.io/RDCore.SDK/api/RDCore.SDK.html) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---
