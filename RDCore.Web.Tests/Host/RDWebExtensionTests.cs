using Microsoft.Extensions.Logging.Abstractions;
using RDCore.SDK.Client;
using RDCore.SDK.Extensibility;
using RDCore.SDK.Extensibility.Diagnostics;
using RDCore.SDK.Model;
using RDCore.SDK.Model.AST;
using RDCore.SDK.Model.AST.Abstract;
using RDCore.SDK.Model.AST.Declarations;
using RDCore.SDK.Model.AST.Directives;
using RDCore.SDK.Model.AST.Expressions;
using RDCore.SDK.Model.Source;
using RDCore.SDK.Model.Values.Intrinsic;
using RDCore.SDK.Platform.Protocol;
using RDCore.SDK.Workspace;
using RDCore.Web.Diagnostics;
using RDCore.Web.Endpoints;
using RDCore.Web.Host;
using System.Reflection;

namespace RDCore.Web.Tests.Host;

/// <summary>
/// RDCore.Web.Host as the platform starts it: the RDWeb extension, which its assembly declares a diagnostics provider of the <c>RDW</c> codes, as
/// <c>rdc describe-ext</c> reads it, and whose handler answers <c>rdcore/diagnostics/document</c> with RDWeb's findings, each linked to its page.
/// </summary>
[TestClass]
public sealed class RDWebExtensionTests
{
    private static readonly Uri File = new("file:///c:/app/Orders.cls");

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void TheHostsAssembly_DeclaresADiagnosticsProvider_OfTheRDWCodes_AndWhereTheirPagesAre()
    {
        var assembly = typeof(RDWebExtensionApp).Assembly;

        CollectionAssert.IsSubsetOf(new[] { nameof(DiagnoseDocument), nameof(DiagnoseProjectModules) },
            ProvidedCorePlatformCapabilities.Reflect(assembly).ToArray());
        var prefix = assembly.GetCustomAttribute<DiagnosticPrefixAttribute>()!;
        Assert.AreEqual(RDWebDiagnostics.Prefix, prefix.Prefix);
        Assert.AreEqual(RDWebDiagnostics.HelpBaseUri, prefix.HelpBaseUri);
    }

    // an analyzer RDCore.Web declares and the extension does not run reports nothing in the editor.
    [TestMethod]
    public void TheExtension_RunsEveryModuleAnalyzerOfRDCoreWeb()
    {
        var declared = AnalyzersOf<IModuleAnalyzer>();
        Assert.IsNotEmpty(declared);

        CollectionAssert.AreEquivalent(declared, RDWebExtensionApp.Analyzers.Select(analyzer => analyzer.GetType()).ToArray());
    }

    [TestMethod]
    public void TheExtension_RunsEveryProjectModulesAnalyzerOfRDCoreWeb()
    {
        var declared = AnalyzersOf<IProjectModulesAnalyzer>();
        Assert.IsNotEmpty(declared);

        CollectionAssert.AreEquivalent(declared, RDWebExtensionApp.ProjectModulesAnalyzers.Select(analyzer => analyzer.GetType()).ToArray());
    }

    private static Type[] AnalyzersOf<TAnalyzer>() => [.. typeof(RDWebDiagnostics).Assembly.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(TAnalyzer).IsAssignableFrom(type))];

    // 1 '@Service("IClock") in SystemClock and FrozenClock, which implement IClock on line 2.
    [TestMethod]
    public async Task TheProjectModulesHandler_ReportsADuplicateServiceName_InEachClass_LinkedToItsPage()
    {
        DiagnoseDocumentPayload Clock(string name, int version)
        {
            var file = new Uri($"file:///c:/app/{name}.cls");
            var parse = new ModuleParseResult
            {
                SyntaxTree = new ModuleNode(new(file.ToString(), [0]), new() { Uri = file, Range = new SourceRange(0, 0, 4, 0) },
                    [new ImplementsDirectiveNode(new(file.ToString(), [1]), new() { Uri = file, Range = new SourceRange(2, 0, 2, 17) },
                        new SimpleNameExpressionNode(new(file.ToString(), [2]), new() { Uri = file, Range = new SourceRange(2, 11, 2, 17) }, "IClock"))]),
                Trivia =
                [
                    new CommentTriviaNode(new(file.ToString(), [3]), new() { Uri = file, Range = new SourceRange(1, 0, 1, 19) }, "'@Service(\"IClock\")"),
                    new AnnotationTriviaNode(new(file.ToString(), [4]), new() { Uri = file, Range = new SourceRange(1, 1, 1, 19) }, RDWebAnnotations.Service,
                        [new LiteralExpressionNode(new(file.ToString(), [5]), new() { Uri = file, Range = new SourceRange(1, 10, 1, 18) },
                            new VBStringValue("IClock"))]),
                ],
            };
            return new DiagnoseDocumentPayload(file, version, parse) { ModuleName = name, ModuleKind = ModuleType.ClassModule };
        }

        var handler = new ProjectModulesAnalyzerDiagnosticsHandler(RDWebExtensionApp.Diagnostics, RDWebExtensionApp.ProjectModulesAnalyzers,
            NullLogger<ProjectModulesAnalyzerDiagnosticsHandler>.Instance);
        var payload = new DiagnoseProjectModulesPayload(new Uri("file:///c:/app/.rdproj"), new RDCoreProject { Name = "App" },
            [Clock("SystemClock", 4), Clock("FrozenClock", 9)]);

        var result = await handler.Handle(new DiagnoseProjectModulesParams { Json = PlatformJson.Serialize(payload) }, TestContext.CancellationToken);

        CollectionAssert.AreEquivalent(new[] { ("SystemClock.cls", 4), ("FrozenClock.cls", 9) },
            result.Modules.Select(module => (module.Document.Segments[^1], module.SourceVersion)).ToArray());
        foreach (var diagnostic in result.Modules.SelectMany(module => module.Diagnostics))
        {
            Assert.AreEqual(RDWebDiagnostics.DuplicateServiceName.ToString(), diagnostic.Code?.String);
            Assert.AreEqual(new Uri($"{RDWebDiagnostics.HelpBaseUri}/rdw00005.html"), diagnostic.CodeDescription?.Href);
        }
    }

    [TestMethod]
    public async Task TheExtensionsHandler_ReportsRDWebsFindings_EachLinkedToItsPage()
    {
        // 0 '@Controller; 1 '@HttpPost; 2-4 Function Place(order, note).
        SyntaxNode[] Annotated(int line, string name)
            => [new CommentTriviaNode(Id(line), Lines(line, line), $"'@{name}"), new AnnotationTriviaNode(Id(line + 100), Lines(line, line), name, [])];
        var procedure = new MemberDeclarationNode(Id(2), Lines(2, 4),
            [new ParameterDeclarationNode(Id(20), Lines(2, 2), "order"), new ParameterDeclarationNode(Id(21), Lines(2, 2), "note")], "Place",
            MemberKind.Function, AccessModifier.Public);
        var parse = new ModuleParseResult
        {
            SyntaxTree = new ModuleNode(Id(0), Lines(0, 4), [procedure]),
            Trivia = [.. Annotated(0, RDWebAnnotations.Controller), .. Annotated(1, RDWebAnnotations.HttpPost)],
        };
        var handler = new AnalyzerDiagnosticsHandler(RDWebExtensionApp.Diagnostics, RDWebExtensionApp.Analyzers,
            NullLogger<AnalyzerDiagnosticsHandler>.Instance);

        var result = await handler.Handle(new DiagnoseDocumentParams { Json = PlatformJson.Serialize(new DiagnoseDocumentPayload(File, 3, parse)) },
            TestContext.CancellationToken);

        CollectionAssert.AreEquivalent(new[] { RDWebDiagnostics.ImplicitBodyBinding.ToString(), RDWebDiagnostics.SeveralBodyParameters.ToString() },
            result.Diagnostics.Select(diagnostic => diagnostic.Code?.String).ToArray());
        foreach (var diagnostic in result.Diagnostics)
        {
            Assert.AreEqual(new Uri($"{RDWebDiagnostics.HelpBaseUri}/{diagnostic.Code?.String?.ToLowerInvariant()}.html"),
                diagnostic.CodeDescription?.Href);
        }
    }

    private static SyntaxNodeId Id(int key) => new(File.ToString(), [key]);

    private static SourceLocation Lines(int first, int last) => new() { Uri = File, Range = new SourceRange(first, 0, last, 40) };
}
