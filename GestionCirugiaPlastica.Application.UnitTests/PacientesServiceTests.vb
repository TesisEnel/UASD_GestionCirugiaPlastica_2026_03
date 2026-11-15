Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class PacientesServiceTests

    Private _factory As TestDbContextFactory
    Private _service As PacientesService

    <TestInitialize>
    Public Sub Inicializar()
        _factory = New TestDbContextFactory()
        _service = New PacientesService(_factory)
    End Sub

    <TestCleanup>
    Public Sub Limpiar()
        _factory.Dispose()
    End Sub

#Region "Helpers"

    ' FechaRegistro se asigna siempre: el DbContext define su default como
    ' HasDefaultValueSql("(getdate())"), función que SQLite no tiene.
    Private Shared Function CrearPaciente(
            Optional nombre As String = "Maria",
            Optional apellido As String = "Perez",
            Optional cedula As String = "001-0000000-1") As Paciente

        Return New Paciente With {
            .Nombre = nombre,
            .Apellido = apellido,
            .Cedula = cedula,
            .FechaRegistro = New DateTime(2026, 1, 15)
        }
    End Function

    ' Inserta directo en la base (sin pasar por el service) para preparar datos.
    Private Function Sembrar(ParamArray pacientes As Paciente()) As Integer()
        Using contexto = _factory.CreateDbContext()
            contexto.Pacientes.AddRange(pacientes)
            contexto.SaveChanges()
        End Using
        Return pacientes.Select(Function(p) p.PacienteId).ToArray()
    End Function

    ' Lee directo de la base para verificar el estado real, no lo que dice el service.
    Private Function LeerTodos() As List(Of Paciente)
        Using contexto = _factory.CreateDbContext()
            Return contexto.Pacientes.AsNoTracking().ToList()
        End Using
    End Function

#End Region

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_PacienteNuevo_LoInsertaEnLaBaseDeDatos() As Task
        Dim paciente = CrearPaciente()

        Dim resultado = Await _service.Guardar(paciente)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual("Maria", guardados(0).Nombre)
        Assert.AreEqual("001-0000000-1", guardados(0).Cedula)
        Assert.IsTrue(guardados(0).PacienteId > 0, "La base debe generar el PacienteId.")
    End Function

    <TestMethod>
    Public Async Function Guardar_PacienteExistente_ActualizaSusDatos() As Task
        Dim id = Sembrar(CrearPaciente())(0)
        Dim modificado = CrearPaciente(nombre:="Maria Jose")
        modificado.PacienteId = id

        Dim resultado = Await _service.Guardar(modificado)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos()
        Assert.AreEqual(1, guardados.Count, "Debe actualizar, no duplicar.")
        Assert.AreEqual("Maria Jose", guardados(0).Nombre)
    End Function

    ' --- REGLAS QUE EL SERVICE HOY NO CUMPLE -------------------------------
    ' Este test describe el comportamiento deseado: una cédula repetida debería
    ' devolver un Result de fallo. Hoy el service deja escapar DbUpdateException
    ' (índice único UQ__Paciente__...), así que este test FALLA y señala el
    ' cambio que hace falta en PacientesService.
    <TestMethod>
    Public Async Function Guardar_CedulaDuplicada_RetornaFalloSinLanzarExcepcion() As Task
        Sembrar(CrearPaciente(cedula:="001-0000000-1"))
        Dim duplicado = CrearPaciente(nombre:="Otra", cedula:="001-0000000-1")

        Dim excepcion As Exception = Nothing
        Dim resultado As Aplicada1.Core.Result = Nothing
        Try
            resultado = Await _service.Guardar(duplicado)
        Catch ex As Exception
            excepcion = ex
        End Try

        Assert.IsNull(excepcion, $"No debería lanzar: {excepcion?.GetType().Name}")
        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, LeerTodos().Count, "No debe insertarse el duplicado.")
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_RetornaElPaciente() As Task
        Dim id = Sembrar(CrearPaciente(nombre:="Ana"))(0)

        Dim resultado = Await _service.Buscar(id)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Ana", resultado.Value.Nombre)
    End Function

    <TestMethod>
    Public Async Function Buscar_IdInexistente_RetornaValorNulo() As Task
        Dim resultado = Await _service.Buscar(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNull(resultado.Value)
    End Function

#End Region

#Region "GetList"

    <TestMethod>
    Public Async Function GetList_ConCriterio_RetornaSoloLosQueCoinciden() As Task
        Sembrar(
            CrearPaciente("Ana", "Lopez", "001-0000000-1"),
            CrearPaciente("Anabel", "Diaz", "001-0000000-2"),
            CrearPaciente("Luis", "Mota", "001-0000000-3"))

        Dim resultado = Await _service.GetList(Function(p) p.Nombre.Contains("Ana"))

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.IsTrue(resultado.Value.All(Function(p) p.Nombre.Contains("Ana")))
    End Function

    <TestMethod>
    Public Async Function GetList_SinCoincidencias_RetornaListaVacia() As Task
        Sembrar(CrearPaciente())

        Dim resultado = Await _service.GetList(Function(p) p.Nombre.Contains("ZZZ"))

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_PacienteExistente_LoQuitaDeLaBaseDeDatos() As Task
        Dim ids = Sembrar(
            CrearPaciente("Ana", "Lopez", "001-0000000-1"),
            CrearPaciente("Luis", "Mota", "001-0000000-2"))

        Dim resultado = Await _service.Eliminar(ids(0))

        Assert.IsTrue(resultado.IsSuccess)
        Dim restantes = LeerTodos()
        Assert.AreEqual(1, restantes.Count)
        Assert.AreEqual(ids(1), restantes(0).PacienteId)
    End Function

    <TestMethod>
    Public Async Function Eliminar_IdInexistente_NoAfectaOtrosRegistros() As Task
        Sembrar(CrearPaciente())

        Await _service.Eliminar(999)

        Assert.AreEqual(1, LeerTodos().Count)
    End Function

    ' --- REGLA QUE EL SERVICE HOY NO CUMPLE --------------------------------
    ' Un paciente con citas no se puede borrar (FK_Citas_Pacientes). Lo deseable
    ' es un Result de fallo; hoy ExecuteDeleteAsync lanza DbUpdateException.
    <TestMethod>
    Public Async Function Eliminar_PacienteConCitas_RetornaFalloSinLanzarExcepcion() As Task
        Dim id = Sembrar(CrearPaciente())(0)
        Using contexto = _factory.CreateDbContext()
            contexto.Citas.Add(New Cita With {
                .PacienteId = id,
                .Fecha = New DateTime(2026, 2, 1),
                .Estado = "Pendiente",
                .EstadoPago = "Pendiente",
                .Costo = 1500D
            })
            contexto.SaveChanges()
        End Using

        Dim excepcion As Exception = Nothing
        Dim resultado As Aplicada1.Core.Result = Nothing
        Try
            resultado = Await _service.Eliminar(id)
        Catch ex As Exception
            excepcion = ex
        End Try

        Assert.IsNull(excepcion, $"No debería lanzar: {excepcion?.GetType().Name}")
        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, LeerTodos().Count, "El paciente debe seguir existiendo.")
    End Function

#End Region

End Class
