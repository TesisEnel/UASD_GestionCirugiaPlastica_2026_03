Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class ControlesPostoperatoriosServiceTests
    Inherits PruebaBase

    Private _service As ControlesPostoperatoriosService

    <TestInitialize>
    Public Sub Iniciar()
        _service = New ControlesPostoperatoriosService(Factory)
    End Sub

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_ControlNuevo_LoInserta() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim control As New ControlesPostoperatorio With {
            .CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase, .DiasPostoperatorio = 7,
            .Evolucion = "Favorable", .Dolor = "Leve", .Inflamacion = "Minima"}

        Dim resultado = Await _service.Guardar(control)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of ControlesPostoperatorio)()
        Assert.AreEqual(1, guardados.Count)
        Assert.IsTrue(guardados(0).ControlId > 0, "La base debe generar el ControlId.")
    End Function

    <TestMethod>
    Public Async Function Guardar_ControlExistente_ActualizaLaEvolucionSinDuplicar() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim original = SembrarControl(Factory, cirugia.CirugiaId)
        Dim modificado As New ControlesPostoperatorio With {
            .ControlId = original.ControlId, .CirugiaId = cirugia.CirugiaId,
            .Fecha = original.Fecha, .Evolucion = "Con inflamacion"}

        Dim resultado = Await _service.Guardar(modificado)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of ControlesPostoperatorio)()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual("Con inflamacion", guardados(0).Evolucion)
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_ControlesPostoperatorios_Cirugias).
    <TestMethod>
    Public Async Function Guardar_CirugiaInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim control As New ControlesPostoperatorio With {.CirugiaId = 999, .Fecha = FechaBase}

        Dim intento = Await Intentar(Function() _service.Guardar(control))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of ControlesPostoperatorio)())
    End Function

    ' REGLA PENDIENTE: sin fecha, SQL Server rechaza 01/01/0001 (fuera de rango de datetime).
    <TestMethod>
    Public Async Function Guardar_SinFecha_RetornaFallo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim control As New ControlesPostoperatorio With {.CirugiaId = cirugia.CirugiaId}

        Dim resultado = Await _service.Guardar(control)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of ControlesPostoperatorio)())
    End Function

    ' REGLA PENDIENTE: Dolor e Inflamacion tienen máximo de 250 caracteres en la base.
    <TestMethod>
    Public Async Function Guardar_DolorODeMasDe250Caracteres_RetornaFallo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim conDolor As New ControlesPostoperatorio With {
            .CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase, .Dolor = New String("x"c, 251)}
        Dim conInflamacion As New ControlesPostoperatorio With {
            .CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase, .Inflamacion = New String("x"c, 251)}

        Dim r1 = Await _service.Guardar(conDolor)
        Dim r2 = Await _service.Guardar(conInflamacion)

        Assert.IsFalse(r1.IsSuccess)
        Assert.IsFalse(r2.IsSuccess)
        Assert.AreEqual(0, Contar(Of ControlesPostoperatorio)())
    End Function

    ' REGLA PROPUESTA: los días postoperatorios no pueden ser negativos.
    <TestMethod>
    Public Async Function Guardar_DiasPostoperatorioNegativos_RetornaFallo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim control As New ControlesPostoperatorio With {
            .CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase, .DiasPostoperatorio = -3}

        Dim resultado = Await _service.Guardar(control)

        Assert.IsFalse(resultado.IsSuccess)
    End Function

    ' REGLA PROPUESTA: el próximo control no puede ser anterior a este control.
    <TestMethod>
    Public Async Function Guardar_ProximoControlAnteriorALaFecha_RetornaFallo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim control As New ControlesPostoperatorio With {
            .CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase, .ProximoControl = FechaBase.AddDays(-5)}

        Dim resultado = Await _service.Guardar(control)

        Assert.IsFalse(resultado.IsSuccess)
    End Function

    ' REGLA PENDIENTE: guardar un control con un ID que no existe no debe intentar insertarlo.
    <TestMethod>
    Public Async Function Guardar_ControlIdInexistente_RetornaFallo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim control As New ControlesPostoperatorio With {
            .ControlId = 999, .CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase}

        Dim intento = Await Intentar(Function() _service.Guardar(control))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    ' BUG PROBABLE: Guardar usa DbSet.Update(control). Si el JSON trae la Cirugia
    ' anidada (como la devuelve Buscar), EF también la marca como modificada.
    <TestMethod>
    Public Async Function Guardar_ConCirugiaAnidada_NoDebeModificarLaCirugia() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim original = SembrarControl(Factory, cirugia.CirugiaId)
        Dim modificado As New ControlesPostoperatorio With {
            .ControlId = original.ControlId, .CirugiaId = cirugia.CirugiaId,
            .Fecha = original.Fecha, .Evolucion = "Nueva",
            .Cirugia = New Cirugia With {
                .CirugiaId = cirugia.CirugiaId, .PacienteId = cirugia.PacienteId,
                .ProcedimientoId = cirugia.ProcedimientoId, .FechaCirugia = cirugia.FechaCirugia,
                .Estado = "Alterado"}}

        Await _service.Guardar(modificado)

        Using contexto = Factory.CreateDbContext()
            Dim enBase = contexto.Cirugias.Find(cirugia.CirugiaId)
            Assert.AreEqual("Pendiente", enBase.Estado, "Guardar un control no debe cambiar los datos de la cirugía.")
        End Using
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyeCirugiaPacienteYProcedimiento() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim control = SembrarControl(Factory, cirugia.CirugiaId)

        Dim resultado = Await _service.Buscar(control.ControlId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.IsNotNull(resultado.Value.Cirugia.Paciente)
        Assert.IsNotNull(resultado.Value.Cirugia.Procedimiento)
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
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim control = SembrarControl(Factory, cirugia.CirugiaId)
        Dim resultado = Await _service.Buscar(control.ControlId)
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
    Public Async Function BuscarPorNombre_CoincideConElPacienteDeLaCirugia() As Task
        Dim ana = SembrarPaciente(Factory, nombre:="Ana", apellido:="Lopez")
        Dim procedimiento = SembrarProcedimiento(Factory)
        Dim cirugiaAna = SembrarCirugia(Factory, ana.PacienteId, procedimiento.ProcedimientoId)
        Dim cirugiaOtro = SembrarCirugiaCompleta(Factory)
        SembrarControl(Factory, cirugiaAna.CirugiaId)
        SembrarControl(Factory, cirugiaOtro.CirugiaId)

        Dim porNombre = Await _service.BuscarPorNombre("Ana")
        Dim porApellido = Await _service.BuscarPorNombre("Lopez")

        Assert.AreEqual(1, porNombre.Value.Count)
        Assert.AreEqual(cirugiaAna.CirugiaId, porNombre.Value(0).CirugiaId)
        Assert.AreEqual(1, porApellido.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_SinCoincidencias_RetornaListaVacia() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        SembrarControl(Factory, cirugia.CirugiaId)

        Dim resultado = Await _service.BuscarPorNombre("Zzz")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCedula_RetornaSoloLosControlesDeEsaCedula() As Task
        Dim cirugiaA = SembrarCirugiaCompleta(Factory, cedula:="001-1111111-1")
        Dim cirugiaB = SembrarCirugiaCompleta(Factory, cedula:="001-2222222-2")
        SembrarControl(Factory, cirugiaA.CirugiaId)
        SembrarControl(Factory, cirugiaA.CirugiaId, FechaBase.AddDays(20))
        SembrarControl(Factory, cirugiaB.CirugiaId)

        Dim resultado = Await _service.BuscarPorCedula("001-1111111-1")

        Assert.AreEqual(2, resultado.Value.Count)
        Assert.IsTrue(resultado.Value.All(Function(c) c.CirugiaId = cirugiaA.CirugiaId))
    End Function

#End Region

#Region "GetList"

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorCirugia() As Task
        Dim cirugiaA = SembrarCirugiaCompleta(Factory)
        Dim cirugiaB = SembrarCirugiaCompleta(Factory)
        SembrarControl(Factory, cirugiaA.CirugiaId)
        SembrarControl(Factory, cirugiaB.CirugiaId)
        SembrarControl(Factory, cirugiaB.CirugiaId, FechaBase.AddDays(20))

        Dim resultado = Await _service.GetList(Function(c) c.CirugiaId = cirugiaB.CirugiaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.IsNotNull(resultado.Value(0).Cirugia.Paciente)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_ControlExistente_LoQuita() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim control = SembrarControl(Factory, cirugia.CirugiaId)

        Dim resultado = Await _service.Eliminar(control.ControlId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of ControlesPostoperatorio)())
    End Function

    <TestMethod>
    Public Async Function Eliminar_IdInexistente_NoAfectaOtrosRegistros() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        SembrarControl(Factory, cirugia.CirugiaId)

        Await _service.Eliminar(999)

        Assert.AreEqual(1, Contar(Of ControlesPostoperatorio)())
    End Function

    ' REGLA PENDIENTE: borrar algo que no existe debe reportar fallo, no éxito.
    <TestMethod>
    Public Async Function Eliminar_IdInexistente_RetornaFallo() As Task
        Dim resultado = Await _service.Eliminar(999)

        Assert.IsFalse(resultado.IsSuccess)
    End Function

#End Region

End Class
