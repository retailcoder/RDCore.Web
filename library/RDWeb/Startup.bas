Attribute VB_Name = "Startup"
'@ModuleDescription("What the host calls at startup, before and after the workspace's Program.Run: workspace code does not call it.")
Option Explicit

'@Description("A builder for Program.Run, over the host's configuration values: a JSON object of strings by key.")
Public Function CreateBuilder(ByVal configuration As Variant) As WebApplicationBuilder
    Dim builder As WebApplicationBuilder
    Set builder = New WebApplicationBuilder
    builder.Initialize configuration
    Set CreateBuilder = builder
End Function

'@Description("What Program.Run recorded with the builder, which the host reads as JSON: services, authentication schemes, policies and filters.")
Public Function Recorded(ByVal builder As WebApplicationBuilder) As Object
    Set Recorded = builder.Recorded
End Function
