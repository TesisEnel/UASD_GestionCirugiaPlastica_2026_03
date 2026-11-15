Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class HistorialEstadosCitasServiceTests
    Inherits PruebaBase

    Private _service As HistorialEstadosCitasServices

    <TestInitialize>
    Public Sub Iniciar()
        _service = New HistorialEstadosCitasServices(Factory)
    End Sub

    ' Cita lista para colgarle historial.
    Private Function NuevaCita() As Cita
        Return SembrarCita(Factory, SembrarPaciente(Factory).PacienteId)
    End Function

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_CambioValido_LoInsertaRecortandoEspacios() As Task
        Dim cita = NuevaCita()
        Dim cambio As New HistorialEstadosCitas With {
            .CitaId = cita.CitaId, .FechaCambio = FechaBase, .EstadoAnterior = " Pendiente ",
            .EstadoNuevo = " Confirmada ", .Observaciones = " Llamó el paciente "}

        Dim resultado = Await _service.Guardar(cambio)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of HistorialEstadosCitas)()
        Assert.AreEqual(1, guardados.Count)
        Assert.IsTrue(guardados(0).HistorialEstadoCitaId > 0, "La base debe generar el Id.")
        Assert.AreEqual(cita.CitaId, guardados(0).CitaId)
        Assert.AreEqual("Pendiente", guardados(0).EstadoAnterior)
        Assert.AreEqual("Confirmada", guardados(0).EstadoNuevo)
        Assert.AreEqual("Llamó el paciente", guardados(0).Observaciones)
        Assert.AreEqual(FechaBase, guardados(0).FechaCambio)
    End Function

    <TestMethod>
    Public Async Function Guardar_SinUsuarioNiEstadoAnterior_LoGuardaConNulos() As Task
        Dim cita = NuevaCita()

        Dim resultado = Await _service.Guardar(New HistorialEstadosCitas With {
            .CitaId = cita.CitaId, .FechaCambio = FechaBase, .EstadoNuevo = "Pendiente"})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardado = LeerTodos(Of HistorialEstadosCitas)()(0)
        Assert.IsNull(guardado.UsuarioId)
        Assert.IsNull(guardado.EstadoAnterior)
    End Function

    <TestMethod>
    Public Async Function Guardar_ConUsuarioActivo_GuardaElUsuario() As Task
        Dim cita = NuevaCita()
        Dim usuario = SembrarUsuario(Factory)

        Dim resultado = Await _service.Guardar(New HistorialEstadosCitas With {
            .CitaId = cita.CitaId, .FechaCambio = FechaBase, .EstadoNuevo = "Confirmada", .UsuarioId = usuario.UsuarioId})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(usuario.UsuarioId, LeerTodos(Of HistorialEstadosCitas)()(0).UsuarioId)
    End Function

    <TestMethod>
    Public Async Function Guardar_SinFecha_LaPoneElServidor() As Task
        Dim cita = NuevaCita()

        Dim resultado = Await _service.Guardar(New HistorialEstadosCitas With {
            .CitaId = cita.CitaId, .EstadoNuevo = "Confirmada"})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsTrue(LeerTodos(Of HistorialEstadosCitas)()(0).FechaCambio > FechaBase,
                      "Sin fecha, debe usar la hora actual del servidor.")
    End Function

    <TestMethod>
    Public Async Function Guardar_Nulo_RetornaFallo() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(Nothing))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function Guardar_SinCita_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New HistorialEstadosCitas With {.EstadoNuevo = "Confirmada"})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of HistorialEstadosCitas)())
    End Function

    <TestMethod>
    Public Async Function Guardar_CitaInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(New HistorialEstadosCitas With {
            .CitaId = 999, .EstadoNuevo = "Confirmada"}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of HistorialEstadosCitas)())
    End Function

    <TestMethod>
    Public Async Function Guardar_EstadoNuevoVacio_RetornaFallo() As Task
        Dim cita = NuevaCita()

        Dim nulo = Await _service.Guardar(New HistorialEstadosCitas With {.CitaId = cita.CitaId})
        Dim vacio = Await _service.Guardar(New HistorialEstadosCitas With {.CitaId = cita.CitaId, .EstadoNuevo = ""})
        Dim espacios = Await _service.Guardar(New HistorialEstadosCitas With {.CitaId = cita.CitaId, .EstadoNuevo = "   "})

        Assert.IsFalse(nulo.IsSuccess)
        Assert.IsFalse(vacio.IsSuccess)
        Assert.IsFalse(espacios.IsSuccess)
        Assert.AreEqual(0, Contar(Of HistorialEstadosCitas)())
    End Function

    ' El historial es de auditoría: no se editan registros existentes.
    <TestMethod>
    Public Async Function Guardar_ConIdYaAsignado_RetornaFalloYNoModificaNada() As Task
        Dim cita = NuevaCita()
        Dim original = SembrarHistorialEstado(Factory, cita.CitaId, "Pendiente", "Confirmada")

        Dim resultado = Await _service.Guardar(New HistorialEstadosCitas With {
            .HistorialEstadoCitaId = original.HistorialEstadoCitaId, .CitaId = cita.CitaId, .EstadoNuevo = "Cancelada"})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual("Confirmada", LeerTodos(Of HistorialEstadosCitas)()(0).EstadoNuevo)
    End Function

    <TestMethod>
    Public Async Function Guardar_TextosDe251Caracteres_RetornaFallo() As Task
        Dim cita = NuevaCita()
        Dim largo = New String("a"c, 251)

        Dim enNuevo = Await _service.Guardar(New HistorialEstadosCitas With {.CitaId = cita.CitaId, .EstadoNuevo = largo})
        Dim enAnterior = Await _service.Guardar(New HistorialEstadosCitas With {
            .CitaId = cita.CitaId, .EstadoNuevo = "Confirmada", .EstadoAnterior = largo})
        Dim enObservaciones = Await _service.Guardar(New HistorialEstadosCitas With {
            .CitaId = cita.CitaId, .EstadoNuevo = "Confirmada", .Observaciones = largo})

        Assert.IsFalse(enNuevo.IsSuccess)
        Assert.IsFalse(enAnterior.IsSuccess)
        Assert.IsFalse(enObservaciones.IsSuccess)
        Assert.AreEqual(0, Contar(Of HistorialEstadosCitas)())
    End Function

    <TestMethod>
    Public Async Function Guardar_TextosDe250Caracteres_SeAceptan() As Task
        Dim cita = NuevaCita()
        Dim limite = New String("a"c, 250)

        Dim resultado = Await _service.Guardar(New HistorialEstadosCitas With {
            .CitaId = cita.CitaId, .EstadoNuevo = limite, .EstadoAnterior = limite, .Observaciones = limite})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of HistorialEstadosCitas)())
    End Function

    <TestMethod>
    Public Async Function Guardar_UsuarioInexistente_RetornaFallo() As Task
        Dim cita = NuevaCita()

        Dim intento = Await Intentar(Function() _service.Guardar(New HistorialEstadosCitas With {
            .CitaId = cita.CitaId, .EstadoNuevo = "Confirmada", .UsuarioId = 999}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of HistorialEstadosCitas)())
    End Function

    <TestMethod>
    Public Async Function Guardar_UsuarioInactivo_RetornaFallo() As Task
        Dim cita = NuevaCita()
        Dim usuario = SembrarUsuario(Factory)

        ' Se desactiva con un UPDATE: al insertar, EF omite Activo=False y la base pone su default (True).
        Using contexto = Factory.CreateDbContext()
            contexto.Usuarios.Find(usuario.UsuarioId).Activo = False
            contexto.SaveChanges()
        End Using

        Dim resultado = Await _service.Guardar(New HistorialEstadosCitas With {
            .CitaId = cita.CitaId, .EstadoNuevo = "Confirmada", .UsuarioId = usuario.UsuarioId})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of HistorialEstadosCitas)())
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_RetornaElRegistro() As Task
        Dim cita = NuevaCita()
        Dim cambio = SembrarHistorialEstado(Factory, cita.CitaId, "Pendiente", "Confirmada")

        Dim resultado = Await _service.Buscar(cambio.HistorialEstadoCitaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Confirmada", resultado.Value.EstadoNuevo)
    End Function

    <TestMethod>
    Public Async Function Buscar_IdInexistente_RetornaValorNulo() As Task
        Dim resultado = Await _service.Buscar(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNull(resultado.Value)
    End Function

#End Region

#Region "Busquedas y listados"

    <TestMethod>
    Public Async Function BuscarPorCita_RetornaSoloEsaCitaDelMasRecienteAlMasAntiguo() As Task
        Dim cita = NuevaCita()
        Dim otra = NuevaCita()
        Dim primero = SembrarHistorialEstado(Factory, cita.CitaId, Nothing, "Pendiente", FechaBase)
        Dim tercero = SembrarHistorialEstado(Factory, cita.CitaId, "Confirmada", "Completada", FechaBase.AddDays(2))
        Dim segundo = SembrarHistorialEstado(Factory, cita.CitaId, "Pendiente", "Confirmada", FechaBase.AddDays(1))
        SembrarHistorialEstado(Factory, otra.CitaId, Nothing, "Pendiente")

        Dim resultado = Await _service.BuscarPorCita(cita.CitaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(3, resultado.Value.Count)
        Assert.AreEqual(tercero.HistorialEstadoCitaId, resultado.Value(0).HistorialEstadoCitaId)
        Assert.AreEqual(segundo.HistorialEstadoCitaId, resultado.Value(1).HistorialEstadoCitaId)
        Assert.AreEqual(primero.HistorialEstadoCitaId, resultado.Value(2).HistorialEstadoCitaId)
    End Function

    ' Con la misma fecha, desempata el Id más alto primero.
    <TestMethod>
    Public Async Function BuscarPorCita_MismaFecha_DesempataPorIdDescendente() As Task
        Dim cita = NuevaCita()
        Dim a = SembrarHistorialEstado(Factory, cita.CitaId, Nothing, "Pendiente", FechaBase)
        Dim b = SembrarHistorialEstado(Factory, cita.CitaId, "Pendiente", "Confirmada", FechaBase)

        Dim resultado = Await _service.BuscarPorCita(cita.CitaId)

        Assert.AreEqual(b.HistorialEstadoCitaId, resultado.Value(0).HistorialEstadoCitaId)
        Assert.AreEqual(a.HistorialEstadoCitaId, resultado.Value(1).HistorialEstadoCitaId)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCita_SinRegistros_RetornaListaVacia() As Task
        Dim resultado = Await _service.BuscarPorCita(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorUsuario_RetornaSoloLosCambiosDeEseUsuario() As Task
        Dim cita = NuevaCita()
        Dim laura = SembrarUsuario(Factory, "Laura")
        Dim pedro = SembrarUsuario(Factory, "Pedro")
        Dim deLauraAntiguo = SembrarHistorialEstado(Factory, cita.CitaId, Nothing, "Pendiente", FechaBase, laura.UsuarioId)
        Dim deLauraReciente = SembrarHistorialEstado(Factory, cita.CitaId, "Pendiente", "Confirmada", FechaBase.AddDays(1), laura.UsuarioId)
        SembrarHistorialEstado(Factory, cita.CitaId, "Confirmada", "Completada", FechaBase.AddDays(2), pedro.UsuarioId)

        Dim resultado = Await _service.BuscarPorUsuario(laura.UsuarioId)

        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual(deLauraReciente.HistorialEstadoCitaId, resultado.Value(0).HistorialEstadoCitaId)
        Assert.AreEqual(deLauraAntiguo.HistorialEstadoCitaId, resultado.Value(1).HistorialEstadoCitaId)
    End Function

    <TestMethod>
    Public Async Function BuscarPorUsuario_SinRegistros_RetornaListaVacia() As Task
        Dim resultado = Await _service.BuscarPorUsuario(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorEstadoNuevo() As Task
        Dim cita = NuevaCita()
        SembrarHistorialEstado(Factory, cita.CitaId, Nothing, "Pendiente")
        SembrarHistorialEstado(Factory, cita.CitaId, "Pendiente", "Cancelada")

        Dim resultado = Await _service.GetList(Function(h) h.EstadoNuevo = "Cancelada")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual("Pendiente", resultado.Value(0).EstadoAnterior)
    End Function

#End Region

#Region "Eliminar"

    ' El historial es de auditoría: no se borra, y debe avisar con un fallo (no con excepción).
    <TestMethod>
    Public Async Function Eliminar_NoSePermite_RetornaFalloYNoBorraNada() As Task
        Dim cita = NuevaCita()
        Dim cambio = SembrarHistorialEstado(Factory, cita.CitaId, Nothing, "Pendiente")

        Dim intento = Await Intentar(Function() _service.Eliminar(cambio.HistorialEstadoCitaId))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of HistorialEstadosCitas)())
    End Function

#End Region

End Class
