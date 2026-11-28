Attribute VB_Name = "BuilderTests"
'@ModuleDescription("What Program.Run records with the builder, as the host reads it back after Startup.Recorded.")
Option Explicit

Public Sub RunAll()
    ServicesAreRecorded_InOrder_WithTheirLifetimes
    AServiceWithNoName_IsError5
    SchemesAndPolicies_AreRecorded
    APolicyWithNeitherARoleNorAClaim_IsError5
    FiltersAreRecorded_InOrder
    TheConfiguration_ReadsAKey_InAnyCase
    TheConfiguration_HasNothing_ForAKeyItHasNot
End Sub

Private Function NewDictionary() As Object
    Set NewDictionary = CreateObject("Scripting.Dictionary")
End Function

Private Sub ServicesAreRecorded_InOrder_WithTheirLifetimes()
    Check.Begin "ServicesAreRecorded_InOrder_WithTheirLifetimes"
    Dim builder As RDWeb.WebApplicationBuilder
    Set builder = RDWeb.Startup.CreateBuilder(Empty)
    builder.Services.AddTransient "IClock", "Services.Clock"
    builder.Services.AddScoped "IOrderRepository", "Services.Orders"
    builder.Services.AddSingleton "IGame", "Services.Game"

    Dim services As Collection
    Set services = RDWeb.Startup.Recorded(builder).Item("services")
    Check.AreEqual 3, services.Count, "services"
    Check.AreEqual "IClock", services(1).Item("name")
    Check.AreEqual "Transient", services(1).Item("lifetime")
    Check.AreEqual "Services.Clock", services(1).Item("factory")
    Check.AreEqual "Scoped", services(2).Item("lifetime")
    Check.AreEqual "IGame", services(3).Item("name")
    Check.AreEqual "Singleton", services(3).Item("lifetime")
End Sub

Private Sub AServiceWithNoName_IsError5()
    Check.Begin "AServiceWithNoName_IsError5"
    Dim builder As RDWeb.WebApplicationBuilder
    Set builder = RDWeb.Startup.CreateBuilder(Empty)
    On Error Resume Next
    builder.Services.AddScoped vbNullString, "Services.Orders"
    Check.AreEqual 5, Err.Number, "no name"
    Err.Clear
    builder.Services.AddScoped "IOrderRepository", vbNullString
    Check.AreEqual 5, Err.Number, "no factory"
    On Error GoTo 0
    Check.AreEqual 0, RDWeb.Startup.Recorded(builder).Item("services").Count, "recorded"
End Sub

Private Sub SchemesAndPolicies_AreRecorded()
    Check.Begin "SchemesAndPolicies_AreRecorded"
    Dim builder As RDWeb.WebApplicationBuilder
    Set builder = RDWeb.Startup.CreateBuilder(Empty)
    builder.Authentication.OpenIdConnect "Default"
    builder.Authorization.AddPolicy "Admins", RequiredRole:="Admin"
    builder.Authorization.AddPolicy "Verified", RequiredClaim:="email_verified", ClaimValue:="true"

    Dim configured As Object
    Set configured = RDWeb.Startup.Recorded(builder)
    Check.AreEqual "OpenIdConnect", configured.Item("authentication")(1).Item("scheme")
    Check.AreEqual "Default", configured.Item("authentication")(1).Item("name")
    Check.AreEqual "Admin", configured.Item("policies")(1).Item("requiredRole")
    Check.AreEqual "email_verified", configured.Item("policies")(2).Item("requiredClaim")
    Check.AreEqual "true", configured.Item("policies")(2).Item("claimValue")
End Sub

Private Sub APolicyWithNeitherARoleNorAClaim_IsError5()
    Check.Begin "APolicyWithNeitherARoleNorAClaim_IsError5"
    Dim builder As RDWeb.WebApplicationBuilder
    Set builder = RDWeb.Startup.CreateBuilder(Empty)
    On Error Resume Next
    builder.Authorization.AddPolicy "Anyone"
    Check.AreEqual 5, Err.Number, "no role, no claim"
    Err.Clear
    builder.Authorization.AddPolicy "Valued", ClaimValue:="true"
    Check.AreEqual 5, Err.Number, "a value with no claim"
    On Error GoTo 0
End Sub

Private Sub FiltersAreRecorded_InOrder()
    Check.Begin "FiltersAreRecorded_InOrder"
    Dim builder As RDWeb.WebApplicationBuilder
    Set builder = RDWeb.Startup.CreateBuilder(Empty)
    builder.UseFilter "Filters.Audit"
    builder.UseFilter "Filters.Throttle"

    Dim filters As Collection
    Set filters = RDWeb.Startup.Recorded(builder).Item("filters")
    Check.AreEqual 2, filters.Count, "filters"
    Check.AreEqual "Filters.Audit", filters(1)
    Check.AreEqual "Filters.Throttle", filters(2)
End Sub

Private Sub TheConfiguration_ReadsAKey_InAnyCase()
    Check.Begin "TheConfiguration_ReadsAKey_InAnyCase"
    Dim values As Object
    Set values = NewDictionary()
    values.Add "RDWeb:Pool:Size", "4"
    Dim builder As RDWeb.WebApplicationBuilder
    Set builder = RDWeb.Startup.CreateBuilder(values)

    Check.AreEqual "4", builder.Configuration.Item("rdweb:pool:size")
    Check.AreEqual "4", builder.Configuration("RDWEB:POOL:SIZE"), "default member"
End Sub

Private Sub TheConfiguration_HasNothing_ForAKeyItHasNot()
    Check.Begin "TheConfiguration_HasNothing_ForAKeyItHasNot"
    Dim builder As RDWeb.WebApplicationBuilder
    Set builder = RDWeb.Startup.CreateBuilder(Empty)
    Check.IsTrue IsEmpty(builder.Configuration("RDWeb:Pool:Size")), "Empty"
End Sub
