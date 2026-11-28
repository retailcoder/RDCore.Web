VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "ClaimsUser"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'@ModuleDescription("The user a request is made by, from the claims its authentication gave: no claims, and not authenticated, when it gave none.")
Option Explicit

Private mIsAuthenticated As Boolean
Private mName As String
Private mClaims As Collection

Private Sub Class_Initialize()
    Set mClaims = New Collection
End Sub

'@Description("Takes the host's user: a JSON object of isAuthenticated, name, and claims, an array of objects of a type and a value.")
Friend Sub Initialize(ByVal user As Variant)
    Set mClaims = New Collection
    mIsAuthenticated = False
    mName = vbNullString
    If Not IsObject(user) Then Exit Sub
    If user Is Nothing Then Exit Sub
    If user.Exists("isAuthenticated") Then mIsAuthenticated = user.Item("isAuthenticated")
    If user.Exists("name") Then
        If Not IsNull(user.Item("name")) Then mName = user.Item("name")
    End If
    If user.Exists("claims") Then
        Dim claims As Variant
        claims = user.Item("claims")
        If IsArray(claims) Then
            Dim index As Long
            For index = LBound(claims) To UBound(claims)
                mClaims.Add claims(index)
            Next
        End If
    End If
End Sub

'@Description("Whether the request's authentication signed a user in.")
Public Property Get IsAuthenticated() As Boolean
    IsAuthenticated = mIsAuthenticated
End Property

'@Description("The user's name, as the identity provider gives it; an empty string when there is none.")
Public Property Get Name() As String
    Name = mName
End Property

'@Description("The value of the user's first claim of a type, such as email; an empty string when the user has none. Claim types are compared with their case.")
Public Function Claim(ByVal claimType As String) As String
    Dim item As Variant
    For Each item In mClaims
        If item.Item("type") = claimType Then
            Claim = item.Item("value")
            Exit Function
        End If
    Next
End Function

'@Description("Whether the user has a role: a claim of the type role whose value is the role, compared with its case.")
Public Function IsInRole(ByVal role As String) As Boolean
    Dim item As Variant
    For Each item In mClaims
        If item.Item("type") = "role" Then
            If item.Item("value") = role Then
                IsInRole = True
                Exit Function
            End If
        End If
    Next
End Function
