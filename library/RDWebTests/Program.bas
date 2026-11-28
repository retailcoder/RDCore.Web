Attribute VB_Name = "Program"
'@ModuleDescription("Runs the RDWeb library's tests: rdc run prints each failure and a count, and ends with an error when a check failed.")
Option Explicit

Public Sub Main()
    BuilderTests.RunAll
    RequestContextTests.RunAll
    ResultsTests.RunAll
    Check.Report
End Sub
