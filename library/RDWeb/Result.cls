VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Result"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'@ModuleDescription("A response that an action, a filter or a hook gives: a status, and the location, detail and body that go with it. Results makes one.")
Option Explicit

Private mStatusCode As Long
Private mLocation As String
Private mDetail As String
Private mBody As Variant

'@Description("Takes what the response is made of.")
Friend Sub Initialize(ByVal statusCode As Long, ByVal location As String, ByVal detail As String, ByVal body As Variant)
    mStatusCode = statusCode
    mLocation = location
    mDetail = detail
    If IsObject(body) Then Set mBody = body Else mBody = body
End Sub

'@Description("The response's HTTP status code.")
Public Property Get StatusCode() As Long
    StatusCode = mStatusCode
End Property

'@Description("The address a redirection or a created resource is at: the response's Location header. An empty string for any other result.")
Public Property Get Location() As String
    Location = mLocation
End Property

'@Description("What a client did wrong, in the problem details of a 400 response. An empty string for any other result.")
Public Property Get Detail() As String
    Detail = mDetail
End Property

'@Description("The value that the response's body is the JSON of; Empty when the response has no body.")
Public Property Get Body() As Variant
    If IsObject(mBody) Then Set Body = mBody Else Body = mBody
End Property
