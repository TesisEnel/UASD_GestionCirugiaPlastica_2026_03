Imports GestionCirugiaPlastica.Infrastructure.Data
Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports System.Threading

''' <summary>
''' IDbContextFactory para pruebas: una base SQLite en memoria por instancia.
''' La conexión se mantiene abierta mientras vive la fábrica, así todos los
''' contextos que cree comparten la misma base (si se cierra, la base desaparece).
''' </summary>
Public NotInheritable Class TestDbContextFactory
    Implements IDbContextFactory(Of GestionCirugiaPlasticaContext)
    Implements IDisposable

    ''' <summary>
    ''' Contexto real + 2 ajustes para SQLite:
    '''  - decimal se guarda como double (SQLite no puede ordenar/comparar decimal).
    ''' </summary>
    Private NotInheritable Class ContextoPrueba
        Inherits GestionCirugiaPlasticaContext

        Public Sub New(opciones As DbContextOptions(Of GestionCirugiaPlasticaContext))
            MyBase.New(opciones)
        End Sub

        Protected Overrides Sub ConfigureConventions(configurationBuilder As ModelConfigurationBuilder)
            MyBase.ConfigureConventions(configurationBuilder)
            configurationBuilder.Properties(Of Decimal)().HaveConversion(Of Double)()
        End Sub
    End Class

    Private ReadOnly _conexion As SqliteConnection
    Private ReadOnly _opciones As DbContextOptions(Of GestionCirugiaPlasticaContext)

    Public Sub New()
        _conexion = New SqliteConnection("DataSource=:memory:")
        _conexion.Open()

        ' El DbContext usa HasDefaultValueSql("(getdate())"), función de SQL Server.
        ' Se registra en SQLite para que los defaults de fecha funcionen igual.
        Dim ahora As Func(Of String) = Function() DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        _conexion.CreateFunction("getdate", ahora)

        _opciones = New DbContextOptionsBuilder(Of GestionCirugiaPlasticaContext)() _
            .UseSqlite(_conexion) _
            .Options

        Using contexto = New ContextoPrueba(_opciones)
            contexto.Database.EnsureCreated()
        End Using
    End Sub

    Public Function CreateDbContext() As GestionCirugiaPlasticaContext _
        Implements IDbContextFactory(Of GestionCirugiaPlasticaContext).CreateDbContext

        Return New ContextoPrueba(_opciones)
    End Function

    Public Function CreateDbContextAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of GestionCirugiaPlasticaContext) _
    Implements IDbContextFactory(Of GestionCirugiaPlasticaContext).CreateDbContextAsync

        Return Task.FromResult(CreateDbContext())
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        _conexion.Dispose()
    End Sub

End Class
