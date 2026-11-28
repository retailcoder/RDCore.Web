VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "ServiceCollection"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'@ModuleDescription("The services Program.Run registers: each one a name, a lifetime, and the function that makes one.")
Option Explicit

Private mRegistrations As Collection

Private Sub Class_Initialize()
    Set mRegistrations = New Collection
End Sub

'@Description("Registers a service made for each parameter that needs it: name is the type a parameter declares, factory a Module.Function that returns one.")
Public Sub AddTransient(ByVal name As String, ByVal factory As String)
    Add name, "Transient", factory, "ServiceCollection.AddTransient"
End Sub

'@Description("Registers a service made once per request: name is the type a parameter declares, factory a Module.Function that returns one.")
Public Sub AddScoped(ByVal name As String, ByVal factory As String)
    Add name, "Scoped", factory, "ServiceCollection.AddScoped"
End Sub

'@Description("Registers a service made once per machine: name is the type a parameter declares, factory a Module.Function that returns one.")
Public Sub AddSingleton(ByVal name As String, ByVal factory As String)
    Add name, "Singleton", factory, "ServiceCollection.AddSingleton"
End Sub

'@Description("The registrations, in the order they were made: each one a name, a lifetime and a factory.")
Friend Function Recorded() As Collection
    Set Recorded = mRegistrations
End Function

Private Sub Add(ByVal name As String, ByVal lifetime As String, ByVal factory As String, ByVal source As String)
    RequireName name, source
    RequireName factory, source
    Dim registration As Object
    Set registration = NewDictionary()
    registration.Add "name", name
    registration.Add "lifetime", lifetime
    registration.Add "factory", factory
    mRegistrations.Add registration
End Sub
