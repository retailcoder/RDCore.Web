using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using RDCore.SDK.Extensibility.Diagnostics;
using RDCore.SDK.Model.AST;
using RDCore.SDK.Model.AST.Abstract;
using RDCore.SDK.Model.AST.Declarations;
using RDCore.SDK.Model.AST.Directives;
using RDCore.SDK.Model.AST.Expressions;
using RDCore.SDK.Model.Source;
using RDCore.SDK.Model.Values.Intrinsic;
using RDCore.SDK.Workspace;
using RDCore.Web.Diagnostics;
using RDCore.Web.Endpoints;

namespace RDCore.Web.Tests.Diagnostics;

/// <summary>
/// <c>RDW00005</c> (<see cref="DuplicateServiceNameAnalyzer"/>): two classes of a project that declare one service name with <c>'@Service</c>, each reported
/// at its annotation, in its own module, with the others as related locations. The parses are built as the parser builds them: each annotation inside the
/// comment line that holds it.
/// </summary>
[TestClass]
public sealed class DuplicateServiceNameTests
{
    private static readonly Uri ProjectFile = new("file:///c:/app/.rdproj");
    private static int _next;

    private static Uri FileOf(string module, string extension = "cls") => new($"file:///c:/app/{module}.{extension}");

    // a class module of the project: its '@Service on line 1 with the arguments, and its Implements statements on line 2.
    private static ModuleAnalysisContext Class(string name, string[] serviceArguments, params string[] implemented)
        => Module(name, FileOf(name), ModuleType.ClassModule, serviceArguments, implemented);

    // a module as the language server sends it down, with its name and its kind.
    private static ModuleAnalysisContext Module(string name, Uri file, ModuleType kind, string[]? serviceArguments, params string[] implemented)
    {
        SyntaxNodeId Id() => new(file.ToString(), [Interlocked.Increment(ref _next)]);
        SourceLocation Line(int line, int end = 40) => new() { Uri = file, Range = new SourceRange(line, 0, line, end) };

        SyntaxNode[] trivia = serviceArguments is null
            ? []
            : [new CommentTriviaNode(Id(), Line(1), "'@Service"), new AnnotationTriviaNode(Id(), Line(1, 30), RDWebAnnotations.Service,
                [.. serviceArguments.Select(argument => new LiteralExpressionNode(Id(), Line(1), new VBStringValue(argument)))])];
        SyntaxNode[] children = [.. implemented.Select(type => new ImplementsDirectiveNode(Id(), Line(2), new SimpleNameExpressionNode(Id(), Line(2), type)))];
        var parse = new ModuleParseResult
        {
            SyntaxTree = new ModuleNode(Id(), new SourceLocation { Uri = file, Range = new SourceRange(0, 0, 30, 0) }, [.. children]),
            Trivia = [.. trivia],
        };
        return new ModuleAnalysisContext(file, parse, null) { ModuleName = name, ModuleKind = kind };
    }

    private static AnalyzerFinding[] Findings(params ModuleAnalysisContext[] modules)
        => [.. new DuplicateServiceNameAnalyzer().Analyze(new ProjectModulesAnalysisContext(ProjectFile, new RDCoreProject { Name = "App" }, modules))];

    [TestMethod]
    public void TwoClassesOfOneServiceName_AreEachReported_AsAnError_AtTheirServiceAnnotation_WithTheOtherAsRelated()
    {
        var findings = Findings(Class("SystemClock", ["IClock"], "IClock"), Class("FrozenClock", ["IClock"], "IClock"), Class("Orders", []));

        Assert.HasCount(2, findings);
        foreach (var (finding, other) in new[] { (Of(findings, "SystemClock"), "FrozenClock"), (Of(findings, "FrozenClock"), "SystemClock") })
        {
            Assert.AreEqual(RDWebDiagnostics.DuplicateServiceName, finding.Code);
            Assert.AreEqual(DiagnosticSeverity.Error, finding.Severity);
            Assert.AreEqual(new SourceRange(1, 0, 1, 30), finding.Range);
            StringAssert.Contains(finding.Message, "IClock");
            StringAssert.Contains(finding.Message, other);
            var related = finding.Related.Single();
            Assert.AreEqual(FileOf(other), related.Document);
            Assert.AreEqual(new SourceRange(1, 0, 1, 30), related.Range);
        }
    }

    // the class's own name, when '@Service has none, and the names in any case.
    [TestMethod]
    [DataRow("Clock")]
    [DataRow("CLOCK")]
    public void AClassesOwnName_AndAnotherClassesNamedService_InAnyCase_AreOneName(string named)
        => Assert.HasCount(2, Findings(Class("Clock", []), Class("FrozenClock", [named], "Clock")));

    [TestMethod]
    public void ThreeClassesOfOneName_AreEachRelatedToTheTwoOthers()
    {
        var findings = Findings(Class("A", ["IClock"], "IClock"), Class("B", ["IClock"], "IClock"), Class("C", ["IClock"], "IClock"));

        Assert.HasCount(3, findings);
        Assert.IsTrue(findings.All(finding => finding.Related.Count == 2 && finding.Related.All(related => related.Document != finding.Document)));
    }

    [TestMethod]
    public void ClassesOfDistinctServiceNames_AreNotReported()
        => Assert.IsEmpty(Findings(Class("SystemClock", ["IClock"], "IClock"), Class("SqlOrders", ["IOrders"], "IOrders"), Class("Orders", [])));

    // the class is named as the module's name says, not as its file is.
    [TestMethod]
    public void AClass_IsNamedByItsModuleName()
    {
        var renamed = Module("FrozenClock", FileOf("clock-test"), ModuleType.ClassModule, ["IClock"], "IClock");

        var finding = Findings(Class("SystemClock", ["IClock"], "IClock"), renamed).Single(each => each.Document == FileOf("SystemClock"));

        StringAssert.Contains(finding.Message, "FrozenClock");
    }

    // a '@Service the host refuses for another reason declares no name: a standard module's, or one that names a type its class does not implement.
    [TestMethod]
    public void AServiceTheHostRefusesForAnotherReason_DeclaresNoName()
        => Assert.IsEmpty(Findings(Class("SystemClock", ["IClock"], "IClock"), Module("Clocks", FileOf("Clocks", "bas"), ModuleType.StdModule, ["IClock"]),
            Class("FrozenClock", ["IClock"])));

    private static AnalyzerFinding Of(AnalyzerFinding[] findings, string module) => findings.Single(finding => finding.Document == FileOf(module));
}
