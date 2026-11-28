VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "IErrorHook"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'@Interface
'@ModuleDescription("A controller that implements it is called when its action raises an unhandled run-time error.")
Option Explicit

'@Description("Runs when the action raises an unhandled run-time error: Nothing answers the error's problem details, and a result is the response.")
Public Function OnError(ByVal request As RequestContext, ByVal number As Long, ByVal description As String) As Result
End Function
