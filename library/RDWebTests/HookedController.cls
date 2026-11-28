VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "HookedController"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
'@ModuleDescription("A controller that opts into the request hook: it refuses a request with no X-Client header.")
Option Explicit
Implements RDWeb.IController
Implements RDWeb.IRequestHook

Private Function IRequestHook_OnRequest(ByVal request As RDWeb.RequestContext) As RDWeb.Result
    If request.Header("X-Client") = vbNullString Then Set IRequestHook_OnRequest = RDWeb.Results.BadRequest("X-Client is missing.")
End Function
