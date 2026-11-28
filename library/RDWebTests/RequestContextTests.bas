Attribute VB_Name = "RequestContextTests"
'@ModuleDescription("The request and user a controller sees, as Requests.ContextOf makes them from the host's request.")
Option Explicit

Public Sub RunAll()
    TheContext_HasTheRequestsMethodPathAndHeaders
    TheUser_HasTheClaimsTheHostGave
    ARequestWithNoUser_IsNotAuthenticated_AndHasNoClaim
End Sub

Private Function NewDictionary() As Object
    Set NewDictionary = CreateObject("Scripting.Dictionary")
End Function

Private Function ClaimOf(ByVal claimType As String, ByVal value As String) As Object
    Set ClaimOf = NewDictionary()
    ClaimOf.Add "type", claimType
    ClaimOf.Add "value", value
End Function

Private Function SignedInRequest() As Object
    Dim headers As Object
    Set headers = NewDictionary()
    headers.Add "X-Client", "web"

    Dim user As Object
    Set user = NewDictionary()
    user.Add "isAuthenticated", True
    user.Add "name", "Ada"
    user.Add "claims", Array(ClaimOf("email", "ada@example.com"), ClaimOf("role", "Admin"))

    Dim request As Object
    Set request = NewDictionary()
    request.Add "method", "GET"
    request.Add "path", "/api/orders/12"
    request.Add "headers", headers
    request.Add "user", user
    Set SignedInRequest = request
End Function

Private Sub TheContext_HasTheRequestsMethodPathAndHeaders()
    Check.Begin "TheContext_HasTheRequestsMethodPathAndHeaders"
    Dim context As RDWeb.RequestContext
    Set context = RDWeb.Requests.ContextOf(SignedInRequest())

    Check.AreEqual "GET", context.Method
    Check.AreEqual "/api/orders/12", context.Path
    Check.AreEqual "web", context.Header("x-client"), "a header in any case"
    Check.AreEqual vbNullString, context.Header("X-Missing"), "a header it has not"
End Sub

Private Sub TheUser_HasTheClaimsTheHostGave()
    Check.Begin "TheUser_HasTheClaimsTheHostGave"
    Dim user As RDWeb.ClaimsUser
    Set user = RDWeb.Requests.ContextOf(SignedInRequest()).User

    Check.IsTrue user.IsAuthenticated, "authenticated"
    Check.AreEqual "Ada", user.Name
    Check.AreEqual "ada@example.com", user.Claim("email")
    Check.IsTrue user.IsInRole("Admin"), "in the role"
    Check.IsTrue Not user.IsInRole("admin"), "a role compared with its case"
    Check.IsTrue Not user.IsInRole("Auditor"), "not in another role"
End Sub

Private Sub ARequestWithNoUser_IsNotAuthenticated_AndHasNoClaim()
    Check.Begin "ARequestWithNoUser_IsNotAuthenticated_AndHasNoClaim"
    Dim request As Object
    Set request = NewDictionary()
    request.Add "method", "POST"
    request.Add "path", "/api/games"

    Dim user As RDWeb.ClaimsUser
    Set user = RDWeb.Requests.ContextOf(request).User
    Check.IsTrue Not user.IsAuthenticated, "not authenticated"
    Check.AreEqual vbNullString, user.Name
    Check.AreEqual vbNullString, user.Claim("email")
    Check.IsTrue Not user.IsInRole("Admin"), "no role"
End Sub
