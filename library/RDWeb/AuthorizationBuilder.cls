VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "AuthorizationBuilder"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'@ModuleDescription("The authorization policies that Program.Run adds, each one named by the '@Authorize annotations that require it.")
Option Explicit

Private mPolicies As Collection

Private Sub Class_Initialize()
    Set mPolicies = New Collection
End Sub

'@Description("Adds a policy a user meets with the role RequiredRole and the claim RequiredClaim, of the value ClaimValue if given. Raises error 5 when name is empty, when neither is given, or when ClaimValue is given without RequiredClaim.")
Public Sub AddPolicy(ByVal name As String, Optional ByVal RequiredRole As String, Optional ByVal RequiredClaim As String, Optional ByVal ClaimValue As String)
    RequireName name, "AuthorizationBuilder.AddPolicy"
    If LenB(RequiredRole) = 0 And LenB(RequiredClaim) = 0 Then Err.Raise 5, "AuthorizationBuilder.AddPolicy"
    If LenB(ClaimValue) <> 0 And LenB(RequiredClaim) = 0 Then Err.Raise 5, "AuthorizationBuilder.AddPolicy"
    Dim policy As Object
    Set policy = NewDictionary()
    policy.Add "name", name
    policy.Add "requiredRole", RequiredRole
    policy.Add "requiredClaim", RequiredClaim
    policy.Add "claimValue", ClaimValue
    mPolicies.Add policy
End Sub

'@Description("The policies, in the order they were added.")
Friend Function Recorded() As Collection
    Set Recorded = mPolicies
End Function
