using RDCore.SDK.Model.AST;
using RDCore.SDK.Model.AST.Abstract;
using RDCore.SDK.Model.AST.Declarations;
using RDCore.SDK.Model.AST.Directives;
using RDCore.SDK.Model.AST.Expressions;
using RDCore.SDK.Model.Source;
using RDCore.SDK.Model.Values.Intrinsic;
using RDCore.Web.Endpoints;
using RDCore.Web.Services;

namespace RDCore.Web.Tests.Services;

/// <summary>
/// The services a program's classes declare with <c>'@Service</c> (<see cref="RDWebServiceReader"/>), and the check that each name is one class's
/// (<c>RDW00005</c>). The parses are built as the parser builds them: each annotation inside the comment line that holds it.
/// </summary>
[TestClass]
public sealed class RDWebServiceReaderTests
{
    private static readonly Uri File = new("file:///c:/app/SqlOrders.cls");
    private static int _next;

    private static SyntaxNodeId Id() => new(File.ToString(), [Interlocked.Increment(ref _next)]);

    private static SourceLocation Lines(int first, int last) => new() { Uri = File, Range = new SourceRange(first, 0, last, 40) };

    // a comment line that holds one '@Service annotation, as the parser keeps it: the comment, then the annotation inside its range.
    private static SyntaxNode[] Service(params string[] arguments)
        => [new CommentTriviaNode(Id(), Lines(1, 1), "'@Service"), new AnnotationTriviaNode(Id(), Lines(1, 1), RDWebAnnotations.Service,
            [.. arguments.Select(argument => new LiteralExpressionNode(Id(), Lines(1, 1), new VBStringValue(argument)))])];

    private static ImplementsDirectiveNode Implements(string name) => new(Id(), Lines(2, 2), new SimpleNameExpressionNode(Id(), Lines(2, 2), name));

    private static AttributeDirectiveNode PredeclaredId(string value) => new(Id(), Lines(0, 0), "VB_PredeclaredId", value, null);

    private static ModuleParseResult Parse(SyntaxNode[] children, SyntaxNode[] trivia)
        => new() { SyntaxTree = new ModuleNode(Id(), Lines(0, 30), [.. children]), Trivia = [.. trivia] };

    private static RDWebService? Read(ModuleParseResult parse, string className = "SqlOrders", ModuleType type = ModuleType.ClassModule)
        => RDWebServiceReader.Read(parse, className, type);

    [TestMethod]
    public void AClassWithAServiceAnnotation_IsAScopedService_OfItsOwnName()
    {
        var service = Read(Parse([], Service()));

        Assert.AreEqual(new RDWebService("SqlOrders", RDWebServiceLifetime.Scoped, "SqlOrders", IsPredeclared: false), service);
    }

    [TestMethod]
    public void AServicesName_IsItsFirstArgument_AnInterfaceTheClassImplements()
    {
        var service = Read(Parse([Implements("IOrderRepository")], Service("IOrderRepository")));

        Assert.AreEqual("IOrderRepository", service!.Name);
        Assert.AreEqual("SqlOrders", service.ClassName);
    }

    [TestMethod]
    [DataRow("Singleton", RDWebServiceLifetime.Singleton)]
    [DataRow("singleton", RDWebServiceLifetime.Singleton)]
    [DataRow("Transient", RDWebServiceLifetime.Transient)]
    [DataRow("SCOPED", RDWebServiceLifetime.Scoped)]
    public void AServicesLifetime_IsItsSecondArgument_InAnyCase(string written, RDWebServiceLifetime lifetime)
        => Assert.AreEqual(lifetime, Read(Parse([], Service("SqlOrders", written)))!.Lifetime);

    [TestMethod]
    [DataRow("Forever")]
    [DataRow("1")]
    [DataRow("")]
    public void AnUnknownLifetime_IsAServiceException_ThatNamesTheClassAndTheLifetime(string written)
    {
        var exception = Assert.ThrowsExactly<RDWebServiceException>(() => Read(Parse([], Service("SqlOrders", written))));

        StringAssert.Contains(exception.Message, "SqlOrders");
        StringAssert.Contains(exception.Message, $"'{written}'");
    }

    [TestMethod]
    public void AServiceNamedForATypeTheClassDoesNotImplement_IsAServiceException_ThatNamesTheClassAndTheType()
    {
        var exception = Assert.ThrowsExactly<RDWebServiceException>(() => Read(Parse([Implements("IClock")], Service("IOrderRepository"))));

        StringAssert.Contains(exception.Message, "SqlOrders");
        StringAssert.Contains(exception.Message, "IOrderRepository");
    }

    [TestMethod]
    public void AServicesName_IsComparedWithoutRegardToCase()
        => Assert.AreEqual("iorderrepository", Read(Parse([Implements("IOrderRepository")], Service("iorderrepository")))!.Name);

    [TestMethod]
    public void AServiceAnnotationInAStandardModule_IsAServiceException_ThatNamesTheModule()
    {
        var exception = Assert.ThrowsExactly<RDWebServiceException>(() => Read(Parse([], Service()), "Orders", ModuleType.StdModule));

        StringAssert.Contains(exception.Message, "Orders");
    }

    [TestMethod]
    public void AClassWithNoServiceAnnotation_IsNoService()
        => Assert.IsNull(Read(Parse([Implements("IOrderRepository")], [])));

    [TestMethod]
    [DataRow("True", true)]
    [DataRow("False", false)]
    public void AServiceIsMadeByItsFactory_OnlyWhenItsClassHasADefaultInstance(string predeclaredId, bool isPredeclared)
        => Assert.AreEqual(isPredeclared, Read(Parse([PredeclaredId(predeclaredId)], Service()))!.IsPredeclared);

    [TestMethod]
    public void TwoClassesOfOneServiceName_InAnyCase_AreAServiceException_ThatNamesBothClassesAndTheName()
    {
        var services = new[]
        {
            new RDWebService("IClock", RDWebServiceLifetime.Singleton, "SystemClock", false),
            new RDWebService("IOrderRepository", RDWebServiceLifetime.Scoped, "SqlOrders", true),
            new RDWebService("iclock", RDWebServiceLifetime.Scoped, "FrozenClock", false),
        };

        var exception = Assert.ThrowsExactly<RDWebServiceException>(() => RDWebServiceReader.Distinct(services));

        StringAssert.Contains(exception.Message, "SystemClock");
        StringAssert.Contains(exception.Message, "FrozenClock");
        StringAssert.Contains(exception.Message, "RDW00005");
    }

    [TestMethod]
    public void ServicesOfDistinctNames_AreKept_InTheOrderTheyAreGiven()
    {
        var services = new[]
        {
            new RDWebService("IOrderRepository", RDWebServiceLifetime.Scoped, "SqlOrders", true),
            new RDWebService("IClock", RDWebServiceLifetime.Singleton, "SystemClock", false),
        };

        CollectionAssert.AreEqual(services, RDWebServiceReader.Distinct(services).ToArray());
    }
}
