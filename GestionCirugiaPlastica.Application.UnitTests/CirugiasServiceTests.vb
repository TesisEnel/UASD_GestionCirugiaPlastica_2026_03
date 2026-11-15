Imports System.Text.Json
Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Text.Json.Serialization

<TestClass>
Public Class CirugiasServiceTests
    Inherits PruebaBase

    Private _service As CirugiasService

    <TestInitialize>
    Public Sub Iniciar()
        _service = New CirugiasService(Factory)
    End Sub

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_CirugiaNueva_LaInserta() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim procedimiento = SembrarProcedimiento(Factory)
        Dim cirugia As New Cirugia With {
            .PacienteId = paciente.PacienteId, .ProcedimientoId = procedimiento.ProcedimientoId,
            .FechaCirugia = FechaBase, .Estado = "Pendiente"}

        Dim resultado = Await _service.Guardar(cirugia)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardadas = LeerTodos(Of Cirugia)()
        Assert.AreEqual(1, guardadas.Count)
        Assert.IsTrue(guardadas(0).CirugiaId > 0, "La base debe generar el CirugiaId.")
    End Function

    <TestMethod>
    Public Async Function Guardar_CirugiaExistente_ActualizaElEstadoSinDuplicar() As Task
        Dim original = SembrarCirugiaCompleta(Factory)
        Dim modificada As New Cirugia With {
            .CirugiaId = original.CirugiaId, .PacienteId = original.PacienteId,
            .ProcedimientoId = original.ProcedimientoId, .FechaCirugia = original.FechaCirugia,
            .Estado = "Completada"}

        Dim resultado = Await _service.Guardar(modificada)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardadas = LeerTodos(Of Cirugia)()
        Assert.AreEqual(1, guardadas.Count)
        Assert.AreEqual("Completada", guardadas(0).Estado)
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_Cirugias_Pacientes).
    <TestMethod>
    Public Async Function Guardar_PacienteInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim procedimiento = SembrarProcedimiento(Factory)
        Dim cirugia As New Cirugia With {
            .PacienteId = 999, .ProcedimientoId = procedimiento.ProcedimientoId,
            .FechaCirugia = FechaBase, .Estado = "Pendiente"}

        Dim intento = Await Intentar(Function() _service.Guardar(cirugia))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Cirugia)())
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_Cirugias_Procedimientos).
    <TestMethod>
    Public Async Function Guardar_ProcedimientoInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim cirugia As New Cirugia With {
            .PacienteId = paciente.PacienteId, .ProcedimientoId = 999,
            .FechaCirugia = FechaBase, .Estado = "Pendiente"}

        Dim intento = Await Intentar(Function() _service.Guardar(cirugia))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    ' BUG PROBABLE: Buscar devuelve la cirugía con Paciente y Procedimiento anidados,
    ' y el cliente normalmente reenvía ese mismo objeto al guardar. Como Guardar usa
    ' DbSet.Update(cirugia), EF también marca como modificado al Paciente anidado
    ' y sobrescribe sus datos con lo que venga en el JSON.
    <TestMethod>
    Public Async Function Guardar_ConPacienteAnidado_NoDebeModificarAlPaciente() As Task
        Dim original = SembrarCirugiaCompleta(Factory)
        Dim modificada As New Cirugia With {
            .CirugiaId = original.CirugiaId, .PacienteId = original.PacienteId,
            .ProcedimientoId = original.ProcedimientoId, .FechaCirugia = original.FechaCirugia,
            .Estado = "Completada",
            .Paciente = New Paciente With {
                .PacienteId = original.PacienteId, .Nombre = "Alterado", .Apellido = "Alterado",
                .FechaRegistro = FechaBase}}

        Await _service.Guardar(modificada)

        Using contexto = Factory.CreateDbContext()
            Dim paciente = contexto.Pacientes.Find(original.PacienteId)
            Assert.AreEqual("Maria", paciente.Nombre, "Guardar una cirugía no debe cambiar los datos del paciente.")
        End Using
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyePacienteYProcedimiento() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)

        Dim resultado = Await _service.Buscar(cirugia.CirugiaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual(cirugia.CirugiaId, resultado.Value.CirugiaId)
        Assert.IsNotNull(resultado.Value.Paciente)
        Assert.IsNotNull(resultado.Value.Procedimiento)
    End Function

    <TestMethod>
    Public Async Function Buscar_IdInexistente_RetornaValorNulo() As Task
        Dim resultado = Await _service.Buscar(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNull(resultado.Value)
    End Function

    ' El controller devuelve este objeto como JSON. Si Paciente -> Cirugia -> Paciente
    ' forman un ciclo, System.Text.Json lanza JsonException y la API responde 500.
    <TestMethod>
    Public Async Function Buscar_ResultadoSePuedeSerializarAJson() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim resultado = Await _service.Buscar(cirugia.CirugiaId)

        Dim excepcion As Exception = Nothing
        Try
            Dim opciones As New JsonSerializerOptions With {.ReferenceHandler = ReferenceHandler.IgnoreCycles}
            Dim json = JsonSerializer.Serialize(resultado.Value, opciones)
        Catch ex As Exception
            excepcion = ex
        End Try

        Assert.IsNull(excepcion, $"No se pudo serializar: {excepcion?.Message}")
    End Function

#End Region

#Region "GetList"

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorEstado() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim procedimiento = SembrarProcedimiento(Factory)
        SembrarCirugia(Factory, paciente.PacienteId, procedimiento.ProcedimientoId, "Pendiente")
        SembrarCirugia(Factory, paciente.PacienteId, procedimiento.ProcedimientoId, "Completada")

        Dim resultado = Await _service.GetList(Function(c) c.Estado.StartsWith("Comp"))

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual("Completada", resultado.Value(0).Estado)
        Assert.IsNotNull(resultado.Value(0).Paciente)
    End Function

    <TestMethod>
    Public Async Function GetList_SinCoincidencias_RetornaListaVacia() As Task
        SembrarCirugiaCompleta(Factory)

        Dim resultado = Await _service.GetList(Function(c) c.PacienteId = 999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_CirugiaExistente_LaQuita() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)

        Dim resultado = Await _service.Eliminar(cirugia.CirugiaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Cirugia)())
    End Function

    <TestMethod>
    Public Async Function Eliminar_IdInexistente_NoAfectaOtrosRegistros() As Task
        SembrarCirugiaCompleta(Factory)

        Await _service.Eliminar(999)

        Assert.AreEqual(1, Contar(Of Cirugia)())
    End Function

    ' REGLA PENDIENTE: una cirugía con registros asociados no se puede borrar (FK);
    ' hoy ExecuteDeleteAsync lanza la excepción.
    <TestMethod>
    Public Async Function Eliminar_CirugiaConPreoperatorios_RetornaFalloSinLanzarExcepcion() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Using contexto = Factory.CreateDbContext()
            contexto.Preoperatorios.Add(New Preoperatorio With {
                .CirugiaId = cirugia.CirugiaId, .FechaEvaluacion = FechaBase, .Autorizado = False})
            contexto.SaveChanges()
        End Using

        Dim intento = Await Intentar(Function() _service.Eliminar(cirugia.CirugiaId))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Cirugia)())
    End Function

#End Region

End Class
