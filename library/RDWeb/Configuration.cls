VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Configuration"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
'@ModuleDescription("The host's configuration, read-only: its values by key, the sections joined by colons, as ASP.NET's configuration names them.")
Option Explicit

Private mValues As Object

Private Sub Class_Initialize()
    Set mValues = NewDictionary()
End Sub

'@Description("Takes the host's values: a JSON object of strings by key. Keys are kept in lowercase, since a configuration's keys are read in any case.")
Friend Sub Initialize(ByVal values As Variant)
    Set mValues = NewDictionary()
    If Not IsObject(values) Then Exit Sub
    If values Is Nothing Then Exit Sub
    Dim key As Variant
    For Each key In values.Keys
        mValues.Item(LCase$(key)) = values.Item(key)
    Next
End Sub

'@DefaultMember
'@Description("A value of the host's configuration, by its key in any case: RDWeb:Pool:Size. Empty when the configuration has no such key.")
Public Property Get Item(ByVal key As String) As Variant
Attribute Item.VB_UserMemId = 0
    If mValues.Exists(LCase$(key)) Then Item = mValues.Item(LCase$(key))
End Property
