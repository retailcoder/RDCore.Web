VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "RequestContext"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'@ModuleDescription("The request a controller is handling, and the user it is made by: a factory takes it as a parameter, and so does an action.")
Option Explicit

Private mMethod As String
Private mPath As String
Private mHeaders As Object
Private mUser As ClaimsUser

Private Sub Class_Initialize()
    Set mHeaders = NewDictionary()
    Set mUser = New ClaimsUser
End Sub

'@Description("Takes the host's request: a JSON object of method, path, headers (an object of strings by name) and user.")
Friend Sub Initialize(ByVal request As Variant)
    Set mHeaders = NewDictionary()
    Set mUser = New ClaimsUser
    mMethod = vbNullString
    mPath = vbNullString
    If Not IsObject(request) Then Exit Sub
    If request Is Nothing Then Exit Sub
    If request.Exists("method") Then mMethod = request.Item("method")
    If request.Exists("path") Then mPath = request.Item("path")
    If request.Exists("headers") Then
        Dim headers As Object
        Set headers = request.Item("headers")
        Dim key As Variant
        For Each key In headers.Keys
            mHeaders.Item(LCase$(key)) = headers.Item(key)
        Next
    End If
    If request.Exists("user") Then mUser.Initialize request.Item("user")
End Sub

'@Description("The request's HTTP method: GET, POST.")
Public Property Get Method() As String
    Method = mMethod
End Property

'@Description("The request's path, from the application's root: /api/orders/12.")
Public Property Get Path() As String
    Path = mPath
End Property

'@Description("The user the request is made by.")
Public Property Get User() As ClaimsUser
    Set User = mUser
End Property

'@Description("A request header's value, by its name in any case; a repeated header's values are joined by commas. An empty string when it is absent.")
Public Function Header(ByVal name As String) As String
    If mHeaders.Exists(LCase$(name)) Then Header = mHeaders.Item(LCase$(name))
End Function
