using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using RDCore.SDK.Extensibility.Diagnostics;
using RDCore.SDK.Model;
using RDCore.SDK.Model.AST;
using RDCore.SDK.Model.AST.Abstract;
using RDCore.SDK.Model.AST.Declarations;
using RDCore.SDK.Model.AST.Directives;
using RDCore.SDK.Model.AST.Expressions;
using RDCore.SDK.Model.Source;
using RDCore.SDK.Model.Symbols.Abstract;
using RDCore.SDK.Model.Values.Intrinsic;
using RDCore.SDK.Platform.Protocol;
using RDCore.SDK.Semantics;
using RDCore.Web.Diagnostics;
using RDCore.Web.Endpoints;

namespace RDCore.Web.Tests.Diagnostics;

/// <summary>
/// <c>RDW00006</c> (<see cref="NonObjectFactoryParameterAnalyzer"/>): each parameter of the factory of a controller or a service with a default instance that
/// is not of an object type. The parses are built as the parser builds them: each parameter with its <c>As</c> clause as its child.
/// </summary>
[TestClass]
public sealed class NonObjectFactoryParameterTests
{
    private static readonly Uri File = new("file:///c:/app/OrdersController.cls");
    private static int _next;

    private static SyntaxNodeId Id() => new(File.ToString(), [Interlocked.Increment(ref _next)]);

    private static SourceLocation Lines(int first, int last, int start = 0, int end = 40)
        => new() { Uri = File, Range = new SourceRange(first, start, last, end) };

    // a parameter written on line 5, each at a column of its own, with the As clause it is declared with: none for "".
    private static ParameterDeclarationNode Parameter(string name, string type, int index, bool isArray = false, bool isParamArray = false,
        string? typeHint = null, string? qualifier = null, string? fixedLength = null)
    {
        var column = 30 + (20 * index);
        SyntaxNode[] clause = type.Length == 0
            ? []
            : [new AsTypeExpressionNode(Id(), Lines(5, 5, column, column + 10), type, qualifier, FixedLength: fixedLength)];
        return new(Id(), Lines(5, 5, column, column + 18), name, ParameterKind.ExplicitByVal, IsParamArray: isParamArray, Children: [.. clause],
            IsArray: isArray, TypeHint: typeHint, NameRange: new SourceRange(5, column, 5, column + name.Length));
    }

    private static MemberDeclarationNode Function(string name, params ParameterDeclarationNode[] parameters)
        => new(Id(), Lines(5, 9), [.. parameters], name, MemberKind.Function, AccessModifier.Public, NameRange: new SourceRange(5, 16, 5, 16 + name.Length));

    private static AttributeDirectiveNode PredeclaredId(string value) => new(Id(), Lines(0, 0), "VB_PredeclaredId", value, null);

    private static SyntaxNode[] Annotated(int line, string name, string? argument = null)
    {
        SyntaxNode[] arguments = argument is null ? [] : [new LiteralExpressionNode(Id(), Lines(line, line), new VBStringValue(argument))];
        return [new CommentTriviaNode(Id(), Lines(line, line), $"'@{name}"), new AnnotationTriviaNode(Id(), Lines(line, line), name, [.. arguments])];
    }

    private static ModuleParseResult Parse(SyntaxNode[] children, params SyntaxNode[][] trivia)
        => new() { SyntaxTree = new ModuleNode(Id(), Lines(0, 30), [.. children]), Trivia = [.. trivia.SelectMany(nodes => nodes)] };

    // a predeclared controller whose Create takes the parameters.
    private static ModuleParseResult Controller(params ParameterDeclarationNode[] parameters)
        => Parse([PredeclaredId("True"), Function("Create", parameters)], Annotated(1, RDWebAnnotations.Controller, "api/orders"));

    // the facts of the factory's parameters: each one's name, and the kind of type it resolved to.
    private static ModuleSemanticsDto Semantics(params (string Name, DeclaredTypeKind? Kind, bool IsArray)[] parameters)
        => ModuleSemanticsDto.From(new ModuleSemanticModel(File, [], [])
        {
            Declarations =
            [
                .. parameters.Select((parameter, index) => new DeclarationFact(
                    new SemanticId(new UriBuilder(File) { Fragment = $"OrdersController.Create.{parameter.Name}" }.Uri), parameter.Name,
                    DeclarationKind.Parameter, AccessModifier.Public, false, Lines(5, 5, 30 + (20 * index), 34 + (20 * index)), null)
                {
                    TypeKind = parameter.Kind,
                    IsArray = parameter.IsArray,
                }),
            ],
        });

    private static AnalyzerFinding[] Findings(ModuleParseResult parse, ModuleSemanticsDto? semantics = null)
        => [.. new NonObjectFactoryParameterAnalyzer().Analyze(new ModuleAnalysisContext(File, parse, semantics))];

    [TestMethod]
    public void AControllersFactory_ReportsAParameterThatIsNotAnObject_AsAnError_AtItsName()
    {
        var findings = Findings(Controller(Parameter("pageSize", "Long", 0), Parameter("orders", "IOrderRepository", 1)));

        var finding = findings.Single();
        Assert.AreEqual(RDWebDiagnostics.NonObjectFactoryParameter, finding.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, finding.Severity);
        Assert.AreEqual(new SourceRange(5, 30, 5, 38), finding.Range);
        StringAssert.Contains(finding.Message, "pageSize");
        StringAssert.Contains(finding.Message, "Create");
    }

    // without the host's facts, the declaration says it: no As clause is a Variant, and so on.
    [TestMethod]
    [DataRow("options", "", false, false, null, null, null)]
    [DataRow("name", "String", false, false, null, null, null)]
    [DataRow("value", "Variant", false, false, null, null, null)]
    [DataRow("count", "Long", false, false, null, "VBA", null)]
    [DataRow("code", "String", false, false, null, null, "10")]
    [DataRow("title", "", false, false, "$", null, null)]
    [DataRow("names", "String", true, false, null, null, null)]
    [DataRow("orders", "IOrderRepository", true, false, null, null, null)]
    [DataRow("values", "Variant", true, true, null, null, null)]
    public void AParameterThatIsNotAnObject_ByItsDeclaration_IsReported(string name, string type, bool isArray, bool isParamArray, string? typeHint,
        string? qualifier, string? fixedLength)
        => Assert.HasCount(1, Findings(Controller(Parameter(name, type, 0, isArray, isParamArray, typeHint, qualifier, fixedLength))));

    [TestMethod]
    [DataRow("IOrderRepository", null)]
    [DataRow("Object", null)]
    [DataRow("RequestContext", "RDWeb")]
    [DataRow("Collection", "VBA")]
    public void AParameterOfAnObjectType_ByItsDeclaration_IsNotReported(string type, string? qualifier)
        => Assert.IsEmpty(Findings(Controller(Parameter("service", type, 0, qualifier: qualifier))));

    // with the host's facts, the resolved type says it: an enum or a UDT names no class, and a type that did not resolve is another diagnostic's.
    [TestMethod]
    [DataRow(DeclaredTypeKind.Enum, false, true)]
    [DataRow(DeclaredTypeKind.UserDefinedType, false, true)]
    [DataRow(DeclaredTypeKind.Intrinsic, false, true)]
    [DataRow(DeclaredTypeKind.Class, true, true)]
    [DataRow(DeclaredTypeKind.Class, false, false)]
    [DataRow(DeclaredTypeKind.Object, false, false)]
    [DataRow(null, false, false)]
    public void WithTheHostsFacts_TheResolvedType_DecidesIt(DeclaredTypeKind? kind, bool isArray, bool reported)
    {
        var findings = Findings(Controller(Parameter("kind", "OrderKind", 0)), Semantics(("kind", kind, isArray)));

        Assert.HasCount(reported ? 1 : 0, findings);
    }

    [TestMethod]
    public void AControllerFactoryAnnotation_NamesTheFactoryThatIsRead()
    {
        var parse = Parse([PredeclaredId("True"), Function("Create", Parameter("size", "Long", 0)), Function("Make", Parameter("count", "Long", 1))],
            Annotated(1, RDWebAnnotations.Controller), Annotated(2, RDWebAnnotations.ControllerFactory, "Make"));

        StringAssert.Contains(Findings(parse).Single().Message, "count");
    }

    [TestMethod]
    public void AServicesCreate_IsReadAsAFactory()
    {
        var parse = Parse([PredeclaredId("True"), Function("Create", Parameter("name", "String", 0))], Annotated(1, RDWebAnnotations.Service));

        Assert.HasCount(1, Findings(parse));
    }

    [TestMethod]
    public void AClassWithNoDefaultInstance_HasNoFactory_AndNothingIsReported()
        => Assert.IsEmpty(Findings(Parse([PredeclaredId("False"), Function("Create", Parameter("size", "Long", 0))],
            Annotated(1, RDWebAnnotations.Controller))));

    [TestMethod]
    public void APredeclaredClassThatIsNeitherAControllerNorAService_IsNotRead()
        => Assert.IsEmpty(Findings(Parse([PredeclaredId("True"), Function("Create", Parameter("size", "Long", 0))])));
}
