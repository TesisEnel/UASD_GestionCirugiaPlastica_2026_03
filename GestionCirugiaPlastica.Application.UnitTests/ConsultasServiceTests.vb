Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class ConsultasServiceTests
    Inherits PruebaBase

    Private _service As ConsultasService

    <TestInitialize>
    Public Sub Iniciar()
        _service = New ConsultasService(Factory)
    End Sub

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_ConsultaNueva_LaInserta() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim consulta As New Consulta With {
            .PacienteId = paciente.PacienteId, .Fecha = FechaBase, .Motivo = "Evaluacion inicial"}

        Dim resultado = Await _service.Guardar(consulta)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardadas = LeerTodos(Of Consulta)()
        Assert.AreEqual(1, guardadas.Count)
        Assert.IsTrue(guardadas(0).ConsultaId > 0, "La base debe generar el ConsultaId.")
    End Function

    <TestMethod>
    Public Async Function Guardar_ConsultaConCitaDelMismoPaciente_LaInserta() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim cita = SembrarCita(Factory, paciente.PacienteId)
        Dim consulta As New Consulta With {
            .PacienteId = paciente.PacienteId, .CitaId = cita.CitaId, .Fecha = FechaBase}

        Dim resultado = Await _service.Guardar(consulta)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(cita.CitaId, LeerTodos(Of Consulta)()(0).CitaId)
    End Function

    <TestMethod>
    Public Async Function Guardar_ConsultaExistente_ActualizaElDiagnosticoSinDuplicar() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim original = SembrarConsulta(Factory, paciente.PacienteId)
        Dim modificada As New Consulta With {
            .ConsultaId = original.ConsultaId, .PacienteId = paciente.PacienteId,
            .Fecha = original.Fecha, .Motivo = original.Motivo, .Diagnostico = "Candidata a cirugia"}

        Dim resultado = Await _service.Guardar(modificada)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardadas = LeerTodos(Of Consulta)()
        Assert.AreEqual(1, guardadas.Count)
        Assert.AreEqual("Candidata a cirugia", guardadas(0).Diagnostico)
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_Consultas_Pacientes).
    <TestMethod>
    Public Async Function Guardar_PacienteInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim consulta As New Consulta With {.PacienteId = 999, .Fecha = FechaBase}

        Dim intento = Await Intentar(Function() _service.Guardar(consulta))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Consulta)())
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_Consultas_Citas).
    <TestMethod>
    Public Async Function Guardar_CitaInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim consulta As New Consulta With {
            .PacienteId = paciente.PacienteId, .CitaId = 999, .Fecha = FechaBase}

        Dim intento = Await Intentar(Function() _service.Guardar(consulta))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    ' REGLA PENDIENTE: la cita de la consulta debe ser del mismo paciente.
    ' Hoy se guarda sin problema una consulta de Ana ligada a la cita de Luis.
    <TestMethod>
    Public Async Function Guardar_CitaDeOtroPaciente_RetornaFallo() As Task
        Dim ana = SembrarPaciente(Factory)
        Dim luis = SembrarPaciente(Factory)
        Dim citaDeLuis = SembrarCita(Factory, luis.PacienteId)
        Dim consulta As New Consulta With {
            .PacienteId = ana.PacienteId, .CitaId = citaDeLuis.CitaId, .Fecha = FechaBase}

        Dim resultado = Await _service.Guardar(consulta)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Consulta)())
    End Function

    ' REGLA PENDIENTE: sin fecha, SQL Server rechaza 01/01/0001 (fuera de rango de datetime).
    <TestMethod>
    Public Async Function Guardar_SinFecha_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim consulta As New Consulta With {.PacienteId = paciente.PacienteId}

        Dim resultado = Await _service.Guardar(consulta)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Consulta)())
    End Function

    ' REGLA PENDIENTE: Motivo tiene máximo de 250 caracteres en la base.
    <TestMethod>
    Public Async Function Guardar_MotivoDeMasDe250Caracteres_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim consulta As New Consulta With {
            .PacienteId = paciente.PacienteId, .Fecha = FechaBase, .Motivo = New String("x"c, 251)}

        Dim resultado = Await _service.Guardar(consulta)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Consulta)())
    End Function

    ' REGLA PENDIENTE: guardar una consulta con un ID que no existe no debe intentar insertarla.
    <TestMethod>
    Public Async Function Guardar_ConsultaIdInexistente_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim consulta As New Consulta With {
            .ConsultaId = 999, .PacienteId = paciente.PacienteId, .Fecha = FechaBase}

        Dim intento = Await Intentar(Function() _service.Guardar(consulta))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    ' BUG PROBABLE: Guardar usa DbSet.Update(consulta). Si el JSON trae el Paciente
    ' anidado (como lo devuelve Buscar), EF también lo marca como modificado.
    <TestMethod>
    Public Async Function Guardar_ConPacienteAnidado_NoDebeModificarAlPaciente() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim original = SembrarConsulta(Factory, paciente.PacienteId)
        Dim modificada As New Consulta With {
            .ConsultaId = original.ConsultaId, .PacienteId = paciente.PacienteId,
            .Fecha = original.Fecha, .Diagnostico = "Nuevo",
            .Paciente = New Paciente With {
                .PacienteId = paciente.PacienteId, .Nombre = "Alterado", .Apellido = "Alterado",
                .FechaRegistro = FechaBase}}

        Await _service.Guardar(modificada)

        Using contexto = Factory.CreateDbContext()
            Dim enBase = contexto.Pacientes.Find(paciente.PacienteId)
            Assert.AreEqual("Maria", enBase.Nombre, "Guardar una consulta no debe cambiar los datos del paciente.")
        End Using
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyePacienteYCita() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim cita = SembrarCita(Factory, paciente.PacienteId)
        Dim consulta = SembrarConsulta(Factory, paciente.PacienteId, cita.CitaId)

        Dim resultado = Await _service.Buscar(consulta.ConsultaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.IsNotNull(resultado.Value.Paciente)
        Assert.IsNotNull(resultado.Value.Cita)
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
        Dim consulta = SembrarConsulta(Factory, paciente.PacienteId, cita.CitaId)
        Dim resultado = Await _service.Buscar(consulta.ConsultaId)
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
        SembrarConsulta(Factory, ana.PacienteId)
        SembrarConsulta(Factory, luis.PacienteId)

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
        SembrarConsulta(Factory, ana.PacienteId)

        Dim resultado = Await _service.BuscarPorNombre("Zzz")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCedula_RetornaSoloLasConsultasDeEsaCedula() As Task
        Dim ana = SembrarPaciente(Factory, cedula:="001-1111111-1")
        Dim luis = SembrarPaciente(Factory, cedula:="001-2222222-2")
        SembrarConsulta(Factory, ana.PacienteId)
        SembrarConsulta(Factory, ana.PacienteId)
        SembrarConsulta(Factory, luis.PacienteId)

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
        SembrarConsulta(Factory, ana.PacienteId)
        SembrarConsulta(Factory, luis.PacienteId)

        Dim resultado = Await _service.GetList(Function(c) c.PacienteId = ana.PacienteId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.IsNotNull(resultado.Value(0).Paciente)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_ConsultaExistente_LaQuita() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim consulta = SembrarConsulta(Factory, paciente.PacienteId)

        Dim resultado = Await _service.Eliminar(consulta.ConsultaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Consulta)())
    End Function

    <TestMethod>
    Public Async Function Eliminar_IdInexistente_NoAfectaOtrosRegistros() As Task
        Dim paciente = SembrarPaciente(Factory)
        SembrarConsulta(Factory, paciente.PacienteId)

        Await _service.Eliminar(999)

        Assert.AreEqual(1, Contar(Of Consulta)())
    End Function

    ' REGLA PENDIENTE: una consulta con recetas no se puede borrar (FK);
    ' hoy ExecuteDeleteAsync lanza la excepción.
    <TestMethod>
    Public Async Function Eliminar_ConsultaConRecetas_RetornaFalloSinLanzarExcepcion() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim consulta = SembrarConsulta(Factory, paciente.PacienteId)
        Using contexto = Factory.CreateDbContext()
            contexto.Recetas.Add(New Receta With {
                .PacienteId = paciente.PacienteId, .ConsultaId = consulta.ConsultaId, .Fecha = FechaBase})
            contexto.SaveChanges()
        End Using

        Dim intento = Await Intentar(Function() _service.Eliminar(consulta.ConsultaId))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Consulta)())
    End Function

#End Region

End Class
