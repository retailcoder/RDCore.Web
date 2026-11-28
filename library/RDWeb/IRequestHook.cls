VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "IRequestHook"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'@Interface
'@ModuleDescription("A controller that implements it is called after its factory and before its action.")
Option Explicit

'@Description("Runs after the controller's factory and before its action: returning Nothing lets the request go on, and a returned result is the response, which ends it.")
Public Function OnRequest(ByVal request As RequestContext) As Result
End Function
