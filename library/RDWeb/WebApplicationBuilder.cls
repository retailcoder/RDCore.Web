VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "WebApplicationBuilder"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'@ModuleDescription("What Program.Run is given: it records the application's services and middleware, which the host builds the application from at startup.")
Option Explicit

Private mServices As ServiceCollection
Private mAuthentication As AuthenticationBuilder
Private mAuthorization As AuthorizationBuilder
Private mConfiguration As Configuration
Private mFilters As Collection

Private Sub Class_Initialize()
    Set mServices = New ServiceCollection
    Set mAuthentication = New AuthenticationBuilder
    Set mAuthorization = New AuthorizationBuilder
    Set mConfiguration = New Configuration
    Set mFilters = New Collection
End Sub

'@Description("Takes the host's configuration values: a JSON object of strings by key.")
Friend Sub Initialize(ByVal values As Variant)
    mConfiguration.Initialize values
End Sub

'@Description("The services the application's factories take.")
Public Property Get Services() As ServiceCollection
    Set Services = mServices
End Property

'@Description("The authentication schemes that sign users in.")
Public Property Get Authentication() As AuthenticationBuilder
    Set Authentication = mAuthentication
End Property

'@Description("The policies that '@Authorize names.")
Public Property Get Authorization() As AuthorizationBuilder
    Set Authorization = mAuthorization
End Property

'@Description("The host's configuration, read-only.")
Public Property Get Configuration() As Configuration
    Set Configuration = mConfiguration
End Property

'@Description("Adds filter, a Module.Function that runs before each controller, in the order filters are added. It is given the RequestContext, and returns Nothing to let the request go on or a result that ends it.")
Public Sub UseFilter(ByVal filter As String)
    RequireName filter, "WebApplicationBuilder.UseFilter"
    mFilters.Add filter
End Sub

'@Description("What was recorded: services, authentication schemes, policies and filters, each in the order it was added.")
Friend Function Recorded() As Object
    Dim configured As Object
    Set configured = NewDictionary()
    configured.Add "services", mServices.Recorded
    configured.Add "authentication", mAuthentication.Recorded
    configured.Add "policies", mAuthorization.Recorded
    configured.Add "filters", mFilters
    Set Recorded = configured
End Function
