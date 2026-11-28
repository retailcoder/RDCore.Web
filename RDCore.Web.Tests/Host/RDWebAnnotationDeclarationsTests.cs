using RDCore.SDK.Extensibility.Annotations;
using RDCore.SDK.Server.Configuration;
using RDCore.Web.Endpoints;
using RDCore.Web.Host;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace RDCore.Web.Tests.Host;

/// <summary>
/// The annotations the RDWeb extension's assembly declares, as <c>rdc describe-ext</c> reads them into its manifest: every one the endpoint reader reads,
/// legal where a controller is written, and described in every language of the platform.
/// </summary>
[TestClass]
public sealed class RDWebAnnotationDeclarationsTests
{
    private static AnnotationSet Read() => ExtensionAnnotationsAttribute.Read(typeof(RDWebExtensionApp).Assembly)
        ?? throw new AssertFailedException("the host's assembly declares no annotations");

    // every annotation name RDWebAnnotations holds, which is what the endpoint reader reads.
    private static string[] ReadByTheEndpointReader() => typeof(RDWebAnnotations)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field is { IsLiteral: true } && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToArray();

    [TestMethod]
    public void EveryAnnotationTheEndpointReaderReads_IsDeclaredByTheHostsAssembly()
    {
        var names = ReadByTheEndpointReader();
        Assert.IsNotEmpty(names);

        CollectionAssert.AreEquivalent(names, Read().Declarations.Select(annotation => annotation.Name).ToArray());
    }

    [TestMethod]
    public void EveryRDWebAnnotation_IsWellFormed_AndCalledByNoOtherOnesName()
    {
        var declarations = Read().Declarations.ToArray();

        Assert.IsEmpty(declarations.Where(annotation => !annotation.IsWellFormed).Select(annotation => annotation.Name));
        Assert.HasCount(declarations.Length, new AnnotationCatalogue([Read()]).Declarations);
    }

    [TestMethod]
    public void EveryRDWebAnnotationAndParameter_IsDescribed_InEveryLanguageOfThePlatform()
    {
        var set = Read();
        var keys = set.Declarations.SelectMany(annotation => annotation.Parameters.Select(parameter => AnnotationSet.KeyOf(annotation, parameter))
            .Prepend(AnnotationSet.KeyOf(annotation))).ToArray();
        var resources = new ResourceManager(RDWebAnnotationDeclarations.DescriptionsResource, typeof(RDWebExtensionApp).Assembly);

        foreach (var language in SettingsSchema.Languages)
        {
            var written = resources.GetResourceSet(CultureInfo.GetCultureInfo(language), createIfNotExists: true, tryParents: false)
                ?? throw new AssertFailedException($"no '{language}' descriptions");
            var writtenKeys = written.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToArray();

            CollectionAssert.AreEquivalent(keys, writtenKeys, $"'{language}': a key with no description, or a description of no annotation");
        }
    }

    // a controller is a standard module or a class module: its annotations are legal there, and in no form or document module.
    [TestMethod]
    [DataRow(RDWebAnnotations.Controller, AnnotationTargets.Module)]
    [DataRow(RDWebAnnotations.KeptMachine, AnnotationTargets.Module)]
    [DataRow(RDWebAnnotations.HttpGet, AnnotationTargets.Member)]
    [DataRow(RDWebAnnotations.HttpPost, AnnotationTargets.Member)]
    [DataRow(RDWebAnnotations.FromBody, AnnotationTargets.Member)]
    [DataRow(RDWebAnnotations.NewMachine, AnnotationTargets.Member)]
    public void AnRDWebAnnotation_IsLegalInAStandardOrAClassModule_AndInNoFormOrDocumentModule(string name, AnnotationTargets target)
    {
        var catalogue = new AnnotationCatalogue([Read()]);
        bool IsLegalIn(AnnotationModuleKinds kind) => catalogue.LegalAt(target, kind).Any(annotation => annotation.IsCalled(name));

        Assert.IsTrue(IsLegalIn(AnnotationModuleKinds.Standard), "standard module");
        Assert.IsTrue(IsLegalIn(AnnotationModuleKinds.Class), "class module");
        Assert.IsFalse(IsLegalIn(AnnotationModuleKinds.Form), "form");
        Assert.IsFalse(IsLegalIn(AnnotationModuleKinds.Document), "document module");
    }

    // a procedure serves an endpoint for each HTTP method annotation, each of its own route.
    [TestMethod]
    [DataRow(RDWebAnnotations.HttpGet)]
    [DataRow(RDWebAnnotations.HttpPost)]
    public void AnHttpMethodAnnotation_MayBeWrittenMoreThanOnce(string name)
        => Assert.IsTrue(new AnnotationCatalogue([Read()]).Find(name)!.Repeats);
}
