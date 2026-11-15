Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class SegurosServicesTests
    Inherits PruebaBase

    Private _service As SegurosServices

    <TestInitialize>
    Public Sub Iniciar()
        _service = New SegurosServices(Factory)
    End Sub

    Private Shared Function Hoy(Optional dias As Integer = 0) As DateOnly
        Return DateOnly.FromDateTime(DateTime.Today.AddDays(dias))
    End Function

#Region "Guardar aseguradora"

    <TestMethod>
    Public Async Function Guardar_SeguroNuevo_LoInsertaActivoRecortandoEspacios() As Task
        Dim seguro As New Seguro With {.Nombre = "  Senasa ", .Telefono = " 809-555-0000 ", .Email = "   "}

        Dim resultado = Await _service.Guardar(seguro)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Seguro)()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual("Senasa", guardados(0).Nombre)
        Assert.AreEqual("809-555-0000", guardados(0).Telefono)
        Assert.IsNull(guardados(0).Email, "Un texto en blanco se guarda como nulo.")
        Assert.IsTrue(guardados(0).Activo, "Un seguro nuevo siempre queda activo.")
        Assert.IsTrue(guardados(0).FechaRegistro.Year >= 2026)
    End Function

    <TestMethod>
    Public Async Function Guardar_NombreVacio_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New Seguro With {.Nombre = "   "})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Seguro)())
    End Function

    <TestMethod>
    Public Async Function Guardar_TextosDeMasDe250Caracteres_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New Seguro With {.Nombre = "A", .Direccion = New String("x"c, 251)})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Seguro)())
    End Function

    <TestMethod>
    Public Async Function Guardar_NombreDuplicadoSinImportarMayusculas_RetornaFallo() As Task
        SembrarSeguro(Factory, "Senasa")

        Dim intento = Await Intentar(Function() _service.Guardar(New Seguro With {.Nombre = "SENASA"}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Seguro)())
    End Function

    <TestMethod>
    Public Async Function Guardar_SeguroExistente_ActualizaDatosPeroNoElEstadoActivo() As Task
        Dim seguro = SembrarSeguro(Factory, "Viejo", activo:=False)

        Dim resultado = Await _service.Guardar(New Seguro With {.SeguroId = seguro.SeguroId, .Nombre = "Nuevo", .Activo = True})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardado = LeerTodos(Of Seguro)()(0)
        Assert.AreEqual("Nuevo", guardado.Nombre)
        Assert.IsFalse(guardado.Activo, "El estado solo cambia con Activar/Desactivar.")
    End Function

    <TestMethod>
    Public Async Function Guardar_ConservandoSuPropioNombre_NoChocaConsigoMismo() As Task
        Dim seguro = SembrarSeguro(Factory, "Senasa")

        Dim resultado = Await _service.Guardar(New Seguro With {.SeguroId = seguro.SeguroId, .Nombre = "Senasa", .Telefono = "809"})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual("809", LeerTodos(Of Seguro)()(0).Telefono)
    End Function

    <TestMethod>
    Public Async Function Guardar_SeguroIdInexistente_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New Seguro With {.SeguroId = 999, .Nombre = "Fantasma"})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Seguro)())
    End Function

#End Region

#Region "Activar y desactivar"

    <TestMethod>
    Public Async Function Desactivar_SeguroActivo_LoDesactiva() As Task
        Dim seguro = SembrarSeguro(Factory)

        Dim resultado = Await _service.Desactivar(seguro.SeguroId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsFalse(LeerTodos(Of Seguro)()(0).Activo)
    End Function

    <TestMethod>
    Public Async Function Activar_SeguroInactivo_LoReactiva() As Task
        Dim seguro = SembrarSeguro(Factory, activo:=False)

        Dim resultado = Await _service.Activar(seguro.SeguroId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsTrue(LeerTodos(Of Seguro)()(0).Activo)
    End Function

    <TestMethod>
    Public Async Function DesactivarYActivar_YaEnEseEstado_RetornanFallo() As Task
        Dim inactivo = SembrarSeguro(Factory, activo:=False)
        Dim activo = SembrarSeguro(Factory)

        Assert.IsFalse((Await _service.Desactivar(inactivo.SeguroId)).IsSuccess, "Desactivar un inactivo")
        Assert.IsFalse((Await _service.Activar(activo.SeguroId)).IsSuccess, "Activar un activo")
    End Function

    <TestMethod>
    Public Async Function DesactivarYActivar_IdInexistente_RetornanFallo() As Task
        Assert.IsFalse((Await _service.Desactivar(999)).IsSuccess)
        Assert.IsFalse((Await _service.Activar(999)).IsSuccess)
    End Function

#End Region

#Region "Consultas de aseguradoras"

    <TestMethod>
    Public Async Function Buscar_IdExistente_RetornaElSeguro() As Task
        Dim seguro = SembrarSeguro(Factory, "Senasa")

        Dim resultado = Await _service.Buscar(seguro.SeguroId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual("Senasa", resultado.Value.Nombre)
    End Function

    <TestMethod>
    Public Async Function Buscar_IdInexistente_RetornaValorNulo() As Task
        Dim resultado = Await _service.Buscar(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNull(resultado.Value)
    End Function

    ' Nota: Contains en SQLite distingue mayúsculas; en SQL Server no. Por eso se usa la misma capitalización.
    <TestMethod>
    Public Async Function BuscarPorNombre_RetornaLosQueContienenElTextoOrdenados() As Task
        SembrarSeguro(Factory, "Seguros Universal")
        SembrarSeguro(Factory, "Seguros Reservas")
        SembrarSeguro(Factory, "Senasa")

        Dim resultado = Await _service.BuscarPorNombre("Seguros")

        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual("Seguros Reservas", resultado.Value(0).Nombre)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_Vacio_RetornaListaVacia() As Task
        SembrarSeguro(Factory, "Senasa")

        Dim resultado = Await _service.BuscarPorNombre("  ")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarActivos_RetornaSoloLosActivosOrdenadosPorNombre() As Task
        SembrarSeguro(Factory, "Zeta")
        SembrarSeguro(Factory, "Alfa")
        SembrarSeguro(Factory, "Inactivo", activo:=False)

        Dim resultado = Await _service.BuscarActivos()

        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual("Alfa", resultado.Value(0).Nombre)
    End Function

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorNombre() As Task
        SembrarSeguro(Factory, "Senasa")
        SembrarSeguro(Factory, "Humano")

        Dim resultado = Await _service.GetList(Function(s) s.Nombre = "Humano")

        Assert.AreEqual(1, resultado.Value.Count)
    End Function

    ' Las aseguradoras no se eliminan: debe reportar fallo (no éxito) y conservarla.
    <TestMethod>
    Public Async Function Eliminar_RetornaFalloYConservaElSeguro() As Task
        Dim seguro = SembrarSeguro(Factory)

        Dim resultado = Await _service.Eliminar(seguro.SeguroId)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Seguro)())
    End Function

#End Region

#Region "Registrar poliza"

    <TestMethod>
    Public Async Function Registrar_PolizaValida_LaInsertaRecortandoEspacios() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)

        Dim resultado = Await _service.RegistrarSeguroPaciente(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId,
            .NumeroPoliza = " POL-9 ", .Titular = "  ", .FechaVencimiento = Hoy(60)})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardada = LeerTodos(Of PacienteSeguro)()
        Assert.AreEqual(1, guardada.Count)
        Assert.AreEqual("POL-9", guardada(0).NumeroPoliza)
        Assert.IsNull(guardada(0).Titular)
        Assert.IsTrue(guardada(0).FechaRegistro.Year >= 2026)
    End Function

    <TestMethod>
    Public Async Function Registrar_VencimientoPasado_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)

        Dim resultado = Await _service.RegistrarSeguroPaciente(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId, .FechaVencimiento = Hoy(-1)})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of PacienteSeguro)())
    End Function

    <TestMethod>
    Public Async Function Registrar_SeguroInactivoOInexistente_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim inactivo = SembrarSeguro(Factory, activo:=False)

        Dim r1 = Await _service.RegistrarSeguroPaciente(New PacienteSeguro With {.PacienteId = paciente.PacienteId, .SeguroId = inactivo.SeguroId})
        Dim r2 = Await _service.RegistrarSeguroPaciente(New PacienteSeguro With {.PacienteId = paciente.PacienteId, .SeguroId = 999})

        Assert.IsFalse(r1.IsSuccess, "Inactivo")
        Assert.IsFalse(r2.IsSuccess, "Inexistente")
        Assert.AreEqual(0, Contar(Of PacienteSeguro)())
    End Function

    <TestMethod>
    Public Async Function Registrar_PacienteInexistente_RetornaFallo() As Task
        Dim seguro = SembrarSeguro(Factory)

        Dim resultado = Await _service.RegistrarSeguroPaciente(New PacienteSeguro With {.PacienteId = 999, .SeguroId = seguro.SeguroId})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of PacienteSeguro)())
    End Function

    <TestMethod>
    Public Async Function Registrar_PolizaDuplicada_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "POL-1")

        Dim resultado = Await _service.RegistrarSeguroPaciente(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId, .NumeroPoliza = " POL-1 "})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of PacienteSeguro)())
    End Function

    <TestMethod>
    Public Async Function Registrar_TextosDeMasDe250Caracteres_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)

        Dim resultado = Await _service.RegistrarSeguroPaciente(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId, .Observaciones = New String("x"c, 251)})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of PacienteSeguro)())
    End Function

#End Region

#Region "Actualizar poliza"

    <TestMethod>
    Public Async Function Actualizar_PolizaValida_ActualizaLosDatosSinDuplicar() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim poliza = SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "POL-1")

        Dim resultado = Await _service.ActualizarSeguroPaciente(New PacienteSeguro With {
            .PacienteSeguroId = poliza.PacienteSeguroId, .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId,
            .NumeroPoliza = "POL-2", .Titular = "Titular"})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardadas = LeerTodos(Of PacienteSeguro)()
        Assert.AreEqual(1, guardadas.Count)
        Assert.AreEqual("POL-2", guardadas(0).NumeroPoliza)
        Assert.AreEqual("Titular", guardadas(0).Titular)
    End Function

    ' Una póliza ya vencida debe poder editarse (titular, observaciones) si no se toca la fecha.
    <TestMethod>
    Public Async Function Actualizar_PolizaVencidaSinCambiarLaFecha_LaActualiza() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim poliza = SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "POL-1", Hoy(-30))

        Dim resultado = Await _service.ActualizarSeguroPaciente(New PacienteSeguro With {
            .PacienteSeguroId = poliza.PacienteSeguroId, .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId,
            .NumeroPoliza = "POL-1", .Titular = "Corregido", .FechaVencimiento = Hoy(-30)})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual("Corregido", LeerTodos(Of PacienteSeguro)()(0).Titular)
    End Function

    ' Renovar: pasar una póliza vencida a una fecha futura debe funcionar.
    <TestMethod>
    Public Async Function Actualizar_RenovarConFechaFutura_LaActualiza() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim poliza = SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "POL-1", Hoy(-30))

        Dim resultado = Await _service.ActualizarSeguroPaciente(New PacienteSeguro With {
            .PacienteSeguroId = poliza.PacienteSeguroId, .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId,
            .NumeroPoliza = "POL-1", .FechaVencimiento = Hoy(365)})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(Hoy(365), LeerTodos(Of PacienteSeguro)()(0).FechaVencimiento)
    End Function

    ' Cambiar a una fecha NUEVA que ya pasó sí se rechaza.
    <TestMethod>
    Public Async Function Actualizar_NuevaFechaPasada_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim poliza = SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "POL-1", Hoy(30))

        Dim resultado = Await _service.ActualizarSeguroPaciente(New PacienteSeguro With {
            .PacienteSeguroId = poliza.PacienteSeguroId, .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId,
            .FechaVencimiento = Hoy(-5)})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(Hoy(30), LeerTodos(Of PacienteSeguro)()(0).FechaVencimiento)
    End Function

    <TestMethod>
    Public Async Function Actualizar_CambiandoElPaciente_RetornaFallo() As Task
        Dim p1 = SembrarPaciente(Factory)
        Dim p2 = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim poliza = SembrarPoliza(Factory, p1.PacienteId, seguro.SeguroId)

        Dim resultado = Await _service.ActualizarSeguroPaciente(New PacienteSeguro With {
            .PacienteSeguroId = poliza.PacienteSeguroId, .PacienteId = p2.PacienteId, .SeguroId = seguro.SeguroId})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(p1.PacienteId, LeerTodos(Of PacienteSeguro)()(0).PacienteId)
    End Function

    <TestMethod>
    Public Async Function Actualizar_PolizaInexistente_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)

        Dim resultado = Await _service.ActualizarSeguroPaciente(New PacienteSeguro With {
            .PacienteSeguroId = 999, .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId})

        Assert.IsFalse(resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function Actualizar_NumeroDeOtraPolizaDelMismoPaciente_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "POL-1")
        Dim segunda = SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "POL-2")

        Dim resultado = Await _service.ActualizarSeguroPaciente(New PacienteSeguro With {
            .PacienteSeguroId = segunda.PacienteSeguroId, .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId,
            .NumeroPoliza = "POL-1"})

        Assert.IsFalse(resultado.IsSuccess)
    End Function

#End Region

#Region "Consultas de polizas"

    <TestMethod>
    Public Async Function BuscarPoliza_IdExistente_IncluyePacienteYSeguro() As Task
        Dim paciente = SembrarPaciente(Factory, nombre:="Ana")
        Dim seguro = SembrarSeguro(Factory, "Senasa")
        Dim poliza = SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId)

        Dim resultado = Await _service.BuscarPoliza(poliza.PacienteSeguroId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual("Ana", resultado.Value.Paciente.Nombre)
        Assert.AreEqual("Senasa", resultado.Value.Seguro.Nombre)
    End Function

    ' Usa la misma configuración de JSON que Program.cs (IgnoreCycles).
    <TestMethod>
    Public Async Function BuscarPoliza_ResultadoSePuedeSerializarAJson() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim poliza = SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId)
        Dim resultado = Await _service.BuscarPoliza(poliza.PacienteSeguroId)
        Dim opciones As New JsonSerializerOptions With {.ReferenceHandler = ReferenceHandler.IgnoreCycles}

        Dim excepcion As Exception = Nothing
        Try
            Dim json = JsonSerializer.Serialize(resultado.Value, opciones)
        Catch ex As Exception
            excepcion = ex
        End Try

        Assert.IsNull(excepcion, $"No se pudo serializar: {excepcion?.Message}")
    End Function

    <TestMethod>
    Public Async Function BuscarPoliza_IdInexistente_RetornaValorNulo() As Task
        Dim resultado = Await _service.BuscarPoliza(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNull(resultado.Value)
    End Function

    <TestMethod>
    Public Async Function BuscarPorPaciente_RetornaSoloLasDeEsePacienteDeLaMasRecienteALaMasAntigua() As Task
        Dim p1 = SembrarPaciente(Factory)
        Dim p2 = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        SembrarPoliza(Factory, p1.PacienteId, seguro.SeguroId, "VIEJA", Nothing, FechaBase)
        SembrarPoliza(Factory, p1.PacienteId, seguro.SeguroId, "NUEVA", Nothing, FechaBase.AddDays(5))
        SembrarPoliza(Factory, p2.PacienteId, seguro.SeguroId, "AJENA", Nothing, FechaBase.AddDays(2))

        Dim resultado = Await _service.BuscarPorPaciente(p1.PacienteId)

        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual("NUEVA", resultado.Value(0).NumeroPoliza)
        Assert.AreEqual("VIEJA", resultado.Value(1).NumeroPoliza)
    End Function

    <TestMethod>
    Public Async Function BuscarPacientes_RetornaSoloLasDeEsaAseguradora() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim s1 = SembrarSeguro(Factory)
        Dim s2 = SembrarSeguro(Factory)
        SembrarPoliza(Factory, paciente.PacienteId, s1.SeguroId, "A")
        SembrarPoliza(Factory, paciente.PacienteId, s2.SeguroId, "B")

        Dim resultado = Await _service.BuscarPacientes(s1.SeguroId)

        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual("A", resultado.Value(0).NumeroPoliza)
    End Function

    <TestMethod>
    Public Async Function BuscarPolizasVencidas_RetornaSoloLasQueYaVencieron() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "VENCIDA", Hoy(-10))
        SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "VIGENTE", Hoy(10))
        SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "SIN-FECHA", Nothing)

        Dim resultado = Await _service.BuscarPolizasVencidas()

        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual("VENCIDA", resultado.Value(0).NumeroPoliza)
    End Function

    <TestMethod>
    Public Async Function BuscarPolizasPorVencer_RetornaLasQueVencenDentroDelPlazo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "PRONTO", Hoy(10))
        SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "LEJOS", Hoy(100))
        SembrarPoliza(Factory, paciente.PacienteId, seguro.SeguroId, "VENCIDA", Hoy(-5))

        Dim resultado = Await _service.BuscarPolizasPorVencer(30)

        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual("PRONTO", resultado.Value(0).NumeroPoliza)
    End Function

    <TestMethod>
    Public Async Function BuscarPolizasPorVencer_PlazoFueraDeRango_RetornaListaVacia() As Task
        Dim resultado = Await _service.BuscarPolizasPorVencer(400)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

#End Region

End Class
