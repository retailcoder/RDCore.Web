Attribute VB_Name = "Check"
'@ModuleDescription("The checks of the RDWeb library's tests: each failure is printed with its test's name, and Report ends a run that had one with an error.")
Option Explicit

Private mTest As String
Private mChecks As Long
Private mFailures As Long

'@Description("Names the test that the next checks belong to.")
Public Sub Begin(ByVal name As String)
    mTest = name
End Sub

'@Description("Fails the test when the two values are not equal.")
Public Sub AreEqual(ByVal expected As Variant, ByVal actual As Variant, Optional ByVal what As String)
    mChecks = mChecks + 1
    If expected <> actual Then Fail "expected " & expected & ", got " & actual & " " & what
End Sub

'@Description("Fails the test when the condition is False.")
Public Sub IsTrue(ByVal condition As Boolean, ByVal what As String)
    mChecks = mChecks + 1
    If Not condition Then Fail what
End Sub

'@Description("Prints how many checks ran and failed, and raises an error when one failed or none ran.")
Public Sub Report()
    Debug.Print mChecks & " checks, " & mFailures & " failed."
    If mFailures > 0 Or mChecks = 0 Then Err.Raise vbObjectError + 1, "Check.Report", mFailures & " of " & mChecks & " checks failed."
End Sub

Private Sub Fail(ByVal message As String)
    mFailures = mFailures + 1
    Debug.Print "FAILED " & mTest & ": " & message
End Sub
