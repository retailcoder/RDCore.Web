using RDCore.SDK.Model;
using RDCore.SDK.Model.AST;
using RDCore.SDK.Model.AST.Abstract;
using RDCore.SDK.Model.AST.Declarations;
using RDCore.SDK.Model.AST.Expressions;
using RDCore.SDK.Model.Source;
using RDCore.SDK.Model.Values.Intrinsic;
using RDCore.SDK.Workspace;
using RDCore.Web.Endpoints;

namespace RDCore.Web.Tests;

// the document of one controller module, its parse built as the parser builds it: '@Controller on line 0 and '@KeptMachine on line 1 when it keeps
// machines, then each action on four lines: its '@NewMachine or a blank line, its HTTP method annotation immediately above it, and a public Function
// of no parameter.
internal static class ControllerDocument
{
    private static int _next;

    public sealed record Action(string Procedure, string HttpMethodAnnotation, string Route, bool NewMachine = false);

    public static RDCoreDocument Of(Uri file, string controllerRoute, string? keptMachineKey, params Action[] actions)
    {
        SyntaxNodeId Id() => new(file.ToString(), [Interlocked.Increment(ref _next)]);
        SourceLocation Line(int first, int last) => new() { Uri = file, Range = new SourceRange(first, 0, last, 40) };
        SyntaxNode[] Annotated(int line, string name, string? argument = null)
        {
            SyntaxNode[] arguments = argument is null ? [] : [new LiteralExpressionNode(Id(), Line(line, line), new VBStringValue(argument))];
            return [new CommentTriviaNode(Id(), Line(line, line), $"'@{name}"), new AnnotationTriviaNode(Id(), Line(line, line), name, [.. arguments])];
        }

        var trivia = new List<SyntaxNode>(Annotated(0, RDWebAnnotations.Controller, controllerRoute));
        if (keptMachineKey is not null)
        {
            trivia.AddRange(Annotated(1, RDWebAnnotations.KeptMachine, keptMachineKey));
        }

        var procedures = new List<SyntaxNode>();
        for (var index = 0; index < actions.Length; index++)
        {
            var first = 2 + 4 * index;
            if (actions[index].NewMachine)
            {
                trivia.AddRange(Annotated(first, RDWebAnnotations.NewMachine));
            }

            trivia.AddRange(Annotated(first + 1, actions[index].HttpMethodAnnotation, actions[index].Route));

            procedures.Add(new MemberDeclarationNode(Id(), Line(first + 2, first + 3), [], actions[index].Procedure, MemberKind.Function,
                AccessModifier.Public));
        }

        var parse = new ModuleParseResult { SyntaxTree = new ModuleNode(Id(), Line(0, 2 + 4 * actions.Length), [.. procedures]), Trivia = [.. trivia] };
        return new RDCoreDocument(file, parse);
    }
}
