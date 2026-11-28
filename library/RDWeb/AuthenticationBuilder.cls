VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "AuthenticationBuilder"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'@ModuleDescription("The authentication schemes Program.Run turns on; each one's options are the host's configuration section RDWeb:Authentication:<name>.")
Option Explicit

Private mSchemes As Collection

Private Sub Class_Initialize()
    Set mSchemes = New Collection
End Sub

'@Description("Signs users in through an OpenID Connect identity provider, with a cookie session; its options are the host's RDWeb:Authentication:<name>.")
Public Sub OpenIdConnect(ByVal name As String)
    Add "OpenIdConnect", name, "AuthenticationBuilder.OpenIdConnect"
End Sub

'@Description("Keeps a signed-in user in a cookie; its options are the host's RDWeb:Authentication:<name>.")
Public Sub Cookies(ByVal name As String)
    Add "Cookies", name, "AuthenticationBuilder.Cookies"
End Sub

'@Description("The schemes, in the order they were turned on: each one a scheme and a name.")
Friend Function Recorded() As Collection
    Set Recorded = mSchemes
End Function

Private Sub Add(ByVal scheme As String, ByVal name As String, ByVal source As String)
    RequireName name, source
    Dim entry As Object
    Set entry = NewDictionary()
    entry.Add "scheme", scheme
    entry.Add "name", name
    mSchemes.Add entry
End Sub
