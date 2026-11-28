Attribute VB_Name = "Requests"
'@ModuleDescription("What the host calls for each request, around the controller: workspace code does not call it.")
Option Explicit

'@Description("The request's context, from the host's request: a JSON object of method, path, headers and user.")
Public Function ContextOf(ByVal request As Variant) As RequestContext
    Dim context As RequestContext
    Set context = New RequestContext
    context.Initialize request
    Set ContextOf = context
End Function

'@Description("What the host answers for a value an action, filter or hook returned: a Result's parts, with isResult True, or the value as the body.")
Public Function ResponseOf(ByVal value As Variant) As Object
    Dim response As Object
    Set response = NewDictionary()
    ' MS-VBAL 5.6.7: TypeOf on Nothing raises error 91.
    Dim isResult As Boolean
    If IsObject(value) Then
        If Not value Is Nothing Then isResult = TypeOf value Is Result
    End If
    If isResult Then
        Dim answered As Result
        Set answered = value
        response.Add "isResult", True
        response.Add "status", answered.StatusCode
        response.Add "location", answered.Location
        response.Add "detail", answered.Detail
        response.Add "body", answered.Body
        Set ResponseOf = response
        Exit Function
    End If
    response.Add "isResult", False
    response.Add "body", value
    Set ResponseOf = response
End Function
