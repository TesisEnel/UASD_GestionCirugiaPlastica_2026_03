Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>Resultado de ejecutar una operación capturando la excepción, si la hubo.</summary>
Public NotInheritable Class Intento(Of T)
    Public Property Resultado As T
    Public Property Excepcion As Exception
End Class

''' <summary>
''' Base de las clases de prueba de services: una base SQLite nueva por test.
''' Los tests derivados crean su service en su propio [TestInitialize].
''' </summary>
Public MustInherit Class PruebaBase

    Protected Property Factory As TestDbContextFactory

    <TestInitialize>
    Public Sub IniciarBase()
        Factory = New TestDbContextFactory()
    End Sub

    <TestCleanup>
    Public Sub LimpiarBase()
        Factory.Dispose()
    End Sub

    ' Lee directo de la base (sin pasar por el service).
    Protected Function LeerTodos(Of T As Class)() As List(Of T)
        Using contexto = Factory.CreateDbContext()
            Return contexto.Set(Of T)().AsNoTracking().ToList()
        End Using
    End Function

    Protected Function Contar(Of T As Class)() As Integer
        Using contexto = Factory.CreateDbContext()
            Return contexto.Set(Of T)().Count()
        End Using
    End Function

    ' Ejecuta la operación y captura la excepción en vez de propagarla.
    ' Se usa en los tests que exigen "devolver fallo, no lanzar excepción".
    Protected Shared Async Function Intentar(Of T)(accion As Func(Of Task(Of T))) As Task(Of Intento(Of T))
        Dim intento As New Intento(Of T)
        Try
            intento.Resultado = Await accion()
        Catch ex As Exception
            intento.Excepcion = ex
        End Try
        Return intento
    End Function

End Class
