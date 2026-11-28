Attribute VB_Name = "Internals"
'@ModuleDescription("The library's own helpers, which a project that references RDWeb does not see.")
Option Explicit
Option Private Module

'@Description("A new Scripting.Dictionary, which the host reads as a JSON object.")
Public Function NewDictionary() As Object
    Set NewDictionary = CreateObject("Scripting.Dictionary")
End Function

'@Description("Raises error 5 (Invalid procedure call or argument), from the member named, when a name is empty.")
Public Sub RequireName(ByVal value As String, ByVal source As String)
    If LenB(value) = 0 Then Err.Raise 5, source
End Sub
