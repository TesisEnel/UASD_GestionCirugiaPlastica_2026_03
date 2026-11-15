Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class CitasServiceTests
    Inherits PruebaBase

    Private _service As CitasService

    <TestInitialize>
    Public Sub Iniciar()
        _service = New CitasService(Factory)
    End Sub

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_CitaNueva_LaInserta() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim cita As New Cita With {
            .PacienteId = paciente.PacienteId, .Fecha = FechaBase, .Costo = 2000D,
            .Estado = "Pendiente", .EstadoPago = "Pendiente"}

        Dim resultado = Await _service.Guardar(cita)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardadas = LeerTodos(Of Cita)()
        Assert.AreEqual(1, guardadas.Count)
        Assert.IsTrue(guardadas(0).CitaId > 0, "La base debe generar el CitaId.")
    End Function

    <TestMethod>
    Public Async Function Guardar_CitaExistente_ActualizaEstadoYCostoSinDuplicar() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim original = SembrarCita(Factory, paciente.PacienteId)
        Dim modificada As New Cita With {
            .CitaId = original.CitaId, .PacienteId = paciente.PacienteId, .Fecha = original.Fecha,
            .Costo = 3000D, .Estado = "Confirmada", .EstadoPago = "Pagada"}

        Dim resultado = Await _service.Guardar(modificada)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardadas = LeerTodos(Of Cita)()
        Assert.AreEqual(1, guardadas.Count)
        Assert.AreEqual("Confirmada", guardadas(0).Estado)
        Assert.AreEqual("Pagada", guardadas(0).EstadoPago)
        Assert.AreEqual(3000D, guardadas(0).Costo)
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_Citas_Pacientes).
    <TestMethod>
    Public Async Function Guardar_PacienteInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim cita As New Cita With {
            .PacienteId = 999, .Fecha = FechaBase, .Costo = 100D,
            .Estado = "Pendiente", .EstadoPago = "Pendiente"}

        Dim intento = Await Intentar(Function() _service.Guardar(cita))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Cita)())
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_Citas_CitaAnterior).
    <TestMethod>
    Public Async Function Guardar_CitaAnteriorInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim cita As New Cita With {
            .PacienteId = paciente.PacienteId, .Fecha = FechaBase, .Costo = 100D,
            .Estado = "Pendiente", .EstadoPago = "Pendiente", .CitaAnteriorId = 999}

        Dim intento = Await Intentar(Function() _service.Guardar(cita))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    ' BUG PROBABLE: Guardar usa DbSet.Update(cita). Si el JSON trae el Paciente
    ' anidado (como lo devuelve Buscar), EF también lo marca como modificado y
    ' sobrescribe los datos del paciente.
    <TestMethod>
    Public Async Function Guardar_ConPacienteAnidado_NoDebeModificarAlPaciente() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim original = SembrarCita(Factory, paciente.PacienteId)
        Dim modificada As New Cita With {
            .CitaId = original.CitaId, .PacienteId = paciente.PacienteId, .Fecha = original.Fecha,
            .Costo = original.Costo, .Estado = "Confirmada", .EstadoPago = "Pendiente",
            .Paciente = New Paciente With {
                .PacienteId = paciente.PacienteId, .Nombre = "Alterado", .Apellido = "Alterado",
                .FechaRegistro = FechaBase}}

        Await _service.Guardar(modificada)

        Using contexto = Factory.CreateDbContext()
            Dim enBase = contexto.Pacientes.Find(paciente.PacienteId)
            Assert.AreEqual("Maria", enBase.Nombre, "Guardar una cita no debe cambiar los datos del paciente.")
        End Using
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyeAlPaciente() As Task
        Dim paciente = SembrarPaciente(Factory, nombre:="Ana")
        Dim cita = SembrarCita(Factory, paciente.PacienteId)

        Dim resultado = Await _service.Buscar(cita.CitaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Ana", resultado.Value.Paciente.Nombre)
    End Function

    <TestMethod>
    Public Async Function Buscar_IdInexistente_RetornaValorNulo() As Task
        Dim resultado = Await _service.Buscar(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNull(resultado.Value)
    End Function

    ' Usa la misma configuración de JSON que Program.cs (IgnoreCycles).
    <TestMethod>
    Public Async Function Buscar_ResultadoSePuedeSerializarAJson() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim cita = SembrarCita(Factory, paciente.PacienteId)
        Dim resultado = Await _service.Buscar(cita.CitaId)
        Dim opciones As New JsonSerializerOptions With {.ReferenceHandler = ReferenceHandler.IgnoreCycles}

        Dim excepcion As Exception = Nothing
        Try
            Dim json = JsonSerializer.Serialize(resultado.Value, opciones)
        Catch ex As Exception
            excepcion = ex
        End Try

        Assert.IsNull(excepcion, $"No se pudo serializar: {excepcion?.Message}")
    End Function

#End Region

#Region "Busquedas por paciente"

    <TestMethod>
    Public Async Function BuscarPorNombre_CoincideConNombreOApellido() As Task
        Dim ana = SembrarPaciente(Factory, nombre:="Ana", apellido:="Lopez")
        Dim luis = SembrarPaciente(Factory, nombre:="Luis", apellido:="Mota")
        SembrarCita(Factory, ana.PacienteId)
        SembrarCita(Factory, luis.PacienteId)

        Dim porNombre = Await _service.BuscarPorNombre("Ana")
        Dim porApellido = Await _service.BuscarPorNombre("Mota")

        Assert.AreEqual(1, porNombre.Value.Count)
        Assert.AreEqual(ana.PacienteId, porNombre.Value(0).PacienteId)
        Assert.AreEqual(1, porApellido.Value.Count)
        Assert.AreEqual(luis.PacienteId, porApellido.Value(0).PacienteId)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_SinCoincidencias_RetornaListaVacia() As Task
        Dim ana = SembrarPaciente(Factory, nombre:="Ana")
        SembrarCita(Factory, ana.PacienteId)

        Dim resultado = Await _service.BuscarPorNombre("Zzz")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCedula_RetornaSoloLasCitasDeEsaCedula() As Task
        Dim ana = SembrarPaciente(Factory, cedula:="001-1111111-1")
        Dim luis = SembrarPaciente(Factory, cedula:="001-2222222-2")
        SembrarCita(Factory, ana.PacienteId)
        SembrarCita(Factory, ana.PacienteId, FechaBase.AddDays(1))
        SembrarCita(Factory, luis.PacienteId)

        Dim resultado = Await _service.BuscarPorCedula("001-1111111-1")

        Assert.AreEqual(2, resultado.Value.Count)
        Assert.IsTrue(resultado.Value.All(Function(c) c.PacienteId = ana.PacienteId))
    End Function

#End Region

#Region "GetList"

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorPaciente() As Task
        Dim ana = SembrarPaciente(Factory)
        Dim luis = SembrarPaciente(Factory)
        SembrarCita(Factory, ana.PacienteId)
        SembrarCita(Factory, luis.PacienteId)

        Dim resultado = Await _service.GetList(Function(c) c.PacienteId = luis.PacienteId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.IsNotNull(resultado.Value(0).Paciente)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_CitaExistente_LaQuita() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim cita = SembrarCita(Factory, paciente.PacienteId)

        Dim resultado = Await _service.Eliminar(cita.CitaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Cita)())
    End Function

    <TestMethod>
    Public Async Function Eliminar_IdInexistente_NoAfectaOtrosRegistros() As Task
        Dim paciente = SembrarPaciente(Factory)
        SembrarCita(Factory, paciente.PacienteId)

        Await _service.Eliminar(999)

        Assert.AreEqual(1, Contar(Of Cita)())
    End Function

    ' REGLA PENDIENTE: una cita con consultas no se puede borrar (FK);
    ' hoy ExecuteDeleteAsync lanza la excepción.
    <TestMethod>
    Public Async Function Eliminar_CitaConConsultas_RetornaFalloSinLanzarExcepcion() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim cita = SembrarCita(Factory, paciente.PacienteId)
        SembrarConsulta(Factory, paciente.PacienteId, cita.CitaId)

        Dim intento = Await Intentar(Function() _service.Eliminar(cita.CitaId))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Cita)())
    End Function

    ' REGLA PENDIENTE: una cita que es "cita anterior" de otra tampoco se puede borrar.
    <TestMethod>
    Public Async Function Eliminar_CitaQueEsAnteriorDeOtra_RetornaFalloSinLanzarExcepcion() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim primera = SembrarCita(Factory, paciente.PacienteId)
        SembrarCita(Factory, paciente.PacienteId, FechaBase.AddDays(7), primera.CitaId)

        Dim intento = Await Intentar(Function() _service.Eliminar(primera.CitaId))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(2, Contar(Of Cita)())
    End Function

#End Region

End Class
