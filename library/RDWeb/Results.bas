Attribute VB_Name = "Results"
'@ModuleDescription("The responses that an action, a filter or a hook gives instead of a value.")
Option Explicit

'@Description("200 OK, with the value as the body's JSON when one is given.")
Public Function Ok(Optional ByVal value As Variant) As Result
    If IsMissing(value) Then value = Empty
    Set Ok = Make(200, vbNullString, vbNullString, value)
End Function

'@Description("201 Created: the resource is at location, and the value, when one is given, is the body's JSON.")
Public Function Created(ByVal location As String, Optional ByVal value As Variant) As Result
    RequireName location, "Results.Created"
    If IsMissing(value) Then value = Empty
    Set Created = Make(201, location, vbNullString, value)
End Function

'@Description("204 No Content.")
Public Function NoContent() As Result
    Set NoContent = Make(204, vbNullString, vbNullString, Empty)
End Function

'@Description("400 Bad Request, with problem details that say what the client did wrong, when detail is given.")
Public Function BadRequest(Optional ByVal detail As String) As Result
    Set BadRequest = Make(400, vbNullString, detail, Empty)
End Function

'@Description("403 Forbidden: the user is known, and may not do this.")
Public Function Forbid() As Result
    Set Forbid = Make(403, vbNullString, vbNullString, Empty)
End Function

'@Description("404 Not Found.")
Public Function NotFound() As Result
    Set NotFound = Make(404, vbNullString, vbNullString, Empty)
End Function

'@Description("302 Found: the client is sent to url.")
Public Function Redirect(ByVal url As String) As Result
    RequireName url, "Results.Redirect"
    Set Redirect = Make(302, url, vbNullString, Empty)
End Function

Private Function Make(ByVal statusCode As Long, ByVal location As String, ByVal detail As String, ByVal body As Variant) As Result
    Dim made As Result
    Set made = New Result
    made.Initialize statusCode, location, detail, body
    Set Make = made
End Function
