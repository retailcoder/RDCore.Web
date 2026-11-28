Attribute VB_Name = "ResultsTests"
'@ModuleDescription("The results a controller returns, how the host reads them back, and the hooks a controller opts into.")
Option Explicit

Public Sub RunAll()
    EachResult_HasItsStatus
    ACreatedOrARedirect_HasItsLocation_AndABadRequestItsDetail
    AResult_IsAnsweredAsOne_WithItsBody
    AnyOtherValue_IsAnsweredAsItsBody
    AControllerHasAHook_OnlyWhenItImplementsItsInterface
End Sub

Private Sub EachResult_HasItsStatus()
    Check.Begin "EachResult_HasItsStatus"
    Check.AreEqual 200, RDWeb.Results.Ok.StatusCode, "Ok"
    Check.AreEqual 201, RDWeb.Results.Created("/api/orders/12").StatusCode, "Created"
    Check.AreEqual 204, RDWeb.Results.NoContent.StatusCode, "NoContent"
    Check.AreEqual 400, RDWeb.Results.BadRequest.StatusCode, "BadRequest"
    Check.AreEqual 403, RDWeb.Results.Forbid.StatusCode, "Forbid"
    Check.AreEqual 404, RDWeb.Results.NotFound.StatusCode, "NotFound"
    Check.AreEqual 302, RDWeb.Results.Redirect("/signed-out").StatusCode, "Redirect"
End Sub

Private Sub ACreatedOrARedirect_HasItsLocation_AndABadRequestItsDetail()
    Check.Begin "ACreatedOrARedirect_HasItsLocation_AndABadRequestItsDetail"
    Check.AreEqual "/api/orders/12", RDWeb.Results.Created("/api/orders/12", 12).Location
    Check.AreEqual "/signed-out", RDWeb.Results.Redirect("/signed-out").Location
    Check.AreEqual "X-Client is missing.", RDWeb.Results.BadRequest("X-Client is missing.").Detail
    Check.AreEqual vbNullString, RDWeb.Results.NotFound.Location, "no location"
End Sub

Private Sub AResult_IsAnsweredAsOne_WithItsBody()
    Check.Begin "AResult_IsAnsweredAsOne_WithItsBody"
    Dim response As Object
    Set response = RDWeb.Requests.ResponseOf(RDWeb.Results.Created("/api/orders/12", 12))

    Check.IsTrue response.Item("isResult"), "a result"
    Check.AreEqual 201, response.Item("status")
    Check.AreEqual "/api/orders/12", response.Item("location")
    Check.AreEqual 12, response.Item("body")
End Sub

Private Sub AnyOtherValue_IsAnsweredAsItsBody()
    Check.Begin "AnyOtherValue_IsAnsweredAsItsBody"
    Dim response As Object
    Set response = RDWeb.Requests.ResponseOf(42)
    Check.IsTrue Not response.Item("isResult"), "a value"
    Check.AreEqual 42, response.Item("body")

    Dim order As Object
    Set order = CreateObject("Scripting.Dictionary")
    Set response = RDWeb.Requests.ResponseOf(order)
    Check.IsTrue Not response.Item("isResult"), "an object that is not a result"
    Check.IsTrue response.Item("body") Is order, "the object as the body"

    Set response = RDWeb.Requests.ResponseOf(Nothing)
    Check.IsTrue Not response.Item("isResult"), "Nothing"
End Sub

Private Sub AControllerHasAHook_OnlyWhenItImplementsItsInterface()
    Check.Begin "AControllerHasAHook_OnlyWhenItImplementsItsInterface"
    Dim hooked As Object
    Set hooked = New HookedController
    Dim plain As Object
    Set plain = New PlainController

    Check.IsTrue TypeOf hooked Is RDWeb.IController, "hooked: a controller"
    Check.IsTrue TypeOf hooked Is RDWeb.IRequestHook, "hooked: a request hook"
    Check.IsTrue Not TypeOf hooked Is RDWeb.IErrorHook, "hooked: no error hook"
    Check.IsTrue TypeOf plain Is RDWeb.IController, "plain: a controller"
    Check.IsTrue Not TypeOf plain Is RDWeb.IRequestHook, "plain: no request hook"

    Dim hook As RDWeb.IRequestHook
    Set hook = hooked
    Check.AreEqual 400, hook.OnRequest(RDWeb.Requests.ContextOf(Empty)).StatusCode, "the hook's result"
End Sub
