Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class PacientesSegurosServiceTests
    Inherits PruebaBase

    Private _service As PacientesSegurosServices

    <TestInitialize>
    Public Sub Iniciar()
        _service = New PacientesSegurosServices(Factory)
    End Sub

#Region "Guardar - registro nuevo"

    <TestMethod>
    Public Async Function Guardar_RegistroNuevo_LoInsertaRecortandoEspacios() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim vence As DateOnly? = New DateOnly(2027, 1, 31)
        Dim registro As New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId,
            .NumeroPoliza = " POL-123 ", .Titular = " Maria Perez ", .Observaciones = " Plan familiar ",
            .FechaVencimiento = vence}

        Dim resultado = Await _service.Guardar(registro)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of PacienteSeguro)()
        Assert.AreEqual(1, guardados.Count)
        Assert.IsTrue(guardados(0).PacienteSeguroId > 0, "La base debe generar el Id.")
        Assert.AreEqual(paciente.PacienteId, guardados(0).PacienteId)
        Assert.AreEqual(seguro.SeguroId, guardados(0).SeguroId)
        Assert.AreEqual("POL-123", guardados(0).NumeroPoliza)
        Assert.AreEqual("Maria Perez", guardados(0).Titular)
        Assert.AreEqual("Plan familiar", guardados(0).Observaciones)
        Assert.AreEqual(vence, guardados(0).FechaVencimiento)
        Assert.IsTrue(guardados(0).FechaRegistro.Year >= 2026, "La fecha de registro la pone la base.")
    End Function

    <TestMethod>
    Public Async Function Guardar_SoloConLosDatosObligatorios_DejaLosOpcionalesNulos() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)

        Dim resultado = Await _service.Guardar(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardado = LeerTodos(Of PacienteSeguro)()(0)
        Assert.IsNull(guardado.NumeroPoliza)
        Assert.IsNull(guardado.Titular)
        Assert.IsNull(guardado.FechaVencimiento)
        Assert.IsNull(guardado.Observaciones)
    End Function

    <TestMethod>
    Public Async Function Guardar_Nulo_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(Nothing))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function Guardar_SinPacienteOSinSeguro_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)

        Dim sinPaciente = Await _service.Guardar(New PacienteSeguro With {.SeguroId = seguro.SeguroId})
        Dim sinSeguro = Await _service.Guardar(New PacienteSeguro With {.PacienteId = paciente.PacienteId})

        Assert.IsFalse(sinPaciente.IsSuccess)
        Assert.IsFalse(sinSeguro.IsSuccess)
        Assert.AreEqual(0, Contar(Of PacienteSeguro)())
    End Function

    <TestMethod>
    Public Async Function Guardar_PacienteInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim seguro = SembrarSeguro(Factory)

        Dim intento = Await Intentar(Function() _service.Guardar(New PacienteSeguro With {
            .PacienteId = 999, .SeguroId = seguro.SeguroId}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of PacienteSeguro)())
    End Function

    <TestMethod>
    Public Async Function Guardar_SeguroInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim paciente = SembrarPaciente(Factory)

        Dim intento = Await Intentar(Function() _service.Guardar(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = 999}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of PacienteSeguro)())
    End Function

    <TestMethod>
    Public Async Function Guardar_SeguroInactivo_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory, activo:=False)

        Dim resultado = Await _service.Guardar(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of PacienteSeguro)())
    End Function

    <TestMethod>
    Public Async Function Guardar_TextosDe251Caracteres_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim largo = New String("a"c, 251)

        Dim enPoliza = Await _service.Guardar(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId, .NumeroPoliza = largo})
        Dim enTitular = Await _service.Guardar(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId, .Titular = largo})
        Dim enObservaciones = Await _service.Guardar(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId, .Observaciones = largo})

        Assert.IsFalse(enPoliza.IsSuccess)
        Assert.IsFalse(enTitular.IsSuccess)
        Assert.IsFalse(enObservaciones.IsSuccess)
        Assert.AreEqual(0, Contar(Of PacienteSeguro)())
    End Function

    <TestMethod>
    Public Async Function Guardar_TextosDe250Caracteres_SeAceptan() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim limite = New String("a"c, 250)

        Dim resultado = Await _service.Guardar(New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId,
            .NumeroPoliza = limite, .Titular = limite, .Observaciones = limite})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of PacienteSeguro)())
    End Function

    ' Si el JSON trae el Paciente anidado, no debe modificarlo.
    <TestMethod>
    Public Async Function Guardar_ConPacienteAnidado_NoDebeModificarAlPaciente() As Task
        Dim paciente = SembrarPaciente(Factory, nombre:="Ana")
        Dim seguro = SembrarSeguro(Factory)
        Dim registro As New PacienteSeguro With {
            .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId,
            .Paciente = New Paciente With {
                .PacienteId = paciente.PacienteId, .Nombre = "Alterado", .Apellido = paciente.Apellido,
                .Cedula = paciente.Cedula, .FechaRegistro = paciente.FechaRegistro}}

        Await _service.Guardar(registro)

        Using contexto = Factory.CreateDbContext()
            Assert.AreEqual("Ana", contexto.Pacientes.Find(paciente.PacienteId).Nombre,
                            "Guardar un seguro de paciente no debe cambiar al paciente.")
        End Using
    End Function

#End Region

#Region "Guardar - registro existente"

    <TestMethod>
    Public Async Function Guardar_RegistroExistente_ActualizaLosDatosSinDuplicar() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim original = SembrarPacienteSeguro(Factory, paciente.PacienteId, seguro.SeguroId, "POL-001")
        Dim vence As DateOnly? = New DateOnly(2028, 6, 30)
        Dim modificado As New PacienteSeguro With {
            .PacienteSeguroId = original.PacienteSeguroId, .PacienteId = paciente.PacienteId,
            .SeguroId = seguro.SeguroId, .NumeroPoliza = " POL-999 ", .Titular = "Otro titular",
            .Observaciones = "Renovada", .FechaVencimiento = vence}

        Dim resultado = Await _service.Guardar(modificado)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of PacienteSeguro)()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual("POL-999", guardados(0).NumeroPoliza)
        Assert.AreEqual("Otro titular", guardados(0).Titular)
        Assert.AreEqual("Renovada", guardados(0).Observaciones)
        Assert.AreEqual(vence, guardados(0).FechaVencimiento)
    End Function

    <TestMethod>
    Public Async Function Guardar_RegistroExistente_ConservaLaFechaDeRegistro() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim original = SembrarPacienteSeguro(Factory, paciente.PacienteId, seguro.SeguroId)

        Await _service.Guardar(New PacienteSeguro With {
            .PacienteSeguroId = original.PacienteSeguroId, .PacienteId = paciente.PacienteId,
            .SeguroId = seguro.SeguroId, .NumeroPoliza = "NUEVA"})

        Assert.AreEqual(FechaBase, LeerTodos(Of PacienteSeguro)()(0).FechaRegistro)
    End Function

    ' Guardar con un ID que no existe no debe intentar insertarlo.
    <TestMethod>
    Public Async Function Guardar_IdInexistente_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)

        Dim intento = Await Intentar(Function() _service.Guardar(New PacienteSeguro With {
            .PacienteSeguroId = 999, .PacienteId = paciente.PacienteId, .SeguroId = seguro.SeguroId}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of PacienteSeguro)())
    End Function

    ' Un registro no se puede pasar de un paciente a otro.
    <TestMethod>
    Public Async Function Guardar_CambiarDePaciente_RetornaFallo() As Task
        Dim p1 = SembrarPaciente(Factory)
        Dim p2 = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory)
        Dim original = SembrarPacienteSeguro(Factory, p1.PacienteId, seguro.SeguroId)

        Dim resultado = Await _service.Guardar(New PacienteSeguro With {
            .PacienteSeguroId = original.PacienteSeguroId, .PacienteId = p2.PacienteId, .SeguroId = seguro.SeguroId})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(p1.PacienteId, LeerTodos(Of PacienteSeguro)()(0).PacienteId)
    End Function

    ' Un registro tampoco se puede pasar de un seguro a otro.
    <TestMethod>
    Public Async Function Guardar_CambiarDeSeguro_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim s1 = SembrarSeguro(Factory)
        Dim s2 = SembrarSeguro(Factory)
        Dim original = SembrarPacienteSeguro(Factory, paciente.PacienteId, s1.SeguroId)

        Dim resultado = Await _service.Guardar(New PacienteSeguro With {
            .PacienteSeguroId = original.PacienteSeguroId, .PacienteId = paciente.PacienteId, .SeguroId = s2.SeguroId})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(s1.SeguroId, LeerTodos(Of PacienteSeguro)()(0).SeguroId)
    End Function

    ' Regla actual del service: si el seguro quedó inactivo, tampoco se edita el registro.
    <TestMethod>
    Public Async Function Guardar_EditarConSeguroInactivo_RetornaFalloYNoModifica() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim seguro = SembrarSeguro(Factory, activo:=False)
        Dim original = SembrarPacienteSeguro(Factory, paciente.PacienteId, seguro.SeguroId, "POL-001")

        Dim resultado = Await _service.Guardar(New PacienteSeguro With {
            .PacienteSeguroId = original.PacienteSeguroId, .PacienteId = paciente.PacienteId,
            .SeguroId = seguro.SeguroId, .NumeroPoliza = "CAMBIADA"})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual("POL-001", LeerTodos(Of PacienteSeguro)()(0).NumeroPoliza)
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyeAlPacienteYAlSeguro() As Task
        Dim paciente = SembrarPaciente(Factory, nombre:="Ana")
        Dim seguro = SembrarSeguro(Factory, "Seguros Norte")
        Dim registro = SembrarPacienteSeguro(Factory, paciente.PacienteId, seguro.SeguroId)

        Dim resultado = Await _service.Buscar(registro.PacienteSeguroId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Ana", resultado.Value.Paciente.Nombre)
        Assert.AreEqual("Seguros Norte", resultado.Value.Seguro.Nombre)
    End Function

    <TestMethod>
    Public Async Function Buscar_IdInexistente_RetornaValorNulo() As Task
        Dim resultado = Await _service.Buscar(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNull(resultado.Value)
    End Function

#End Region

#Region "Listados"

    <TestMethod>
    Public Async Function BuscarPorPaciente_RetornaSoloLosDeEsePacienteDelMasRecienteAlMasAntiguo() As Task
        Dim ana = SembrarPaciente(Factory)
        Dim luis = SembrarPaciente(Factory)
        Dim s1 = SembrarSeguro(Factory)
        Dim s2 = SembrarSeguro(Factory)
        Dim s3 = SembrarSeguro(Factory)
        Dim antiguo = SembrarPacienteSeguro(Factory, ana.PacienteId, s1.SeguroId, "A", FechaBase)
        Dim reciente = SembrarPacienteSeguro(Factory, ana.PacienteId, s2.SeguroId, "B", FechaBase.AddDays(5))
        SembrarPacienteSeguro(Factory, luis.PacienteId, s3.SeguroId, "C")

        Dim resultado = Await _service.BuscarPorPaciente(ana.PacienteId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual(reciente.PacienteSeguroId, resultado.Value(0).PacienteSeguroId)
        Assert.AreEqual(antiguo.PacienteSeguroId, resultado.Value(1).PacienteSeguroId)
        Assert.IsNotNull(resultado.Value(0).Seguro, "Debe incluir el seguro.")
    End Function

    <TestMethod>
    Public Async Function BuscarPorPaciente_SinRegistros_RetornaListaVacia() As Task
        Dim resultado = Await _service.BuscarPorPaciente(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorPolizaEIncluyePacienteYSeguro() As Task
        Dim seguro = SembrarSeguro(Factory)
        SembrarPacienteSeguro(Factory, SembrarPaciente(Factory).PacienteId, seguro.SeguroId, "POL-A")
        SembrarPacienteSeguro(Factory, SembrarPaciente(Factory).PacienteId, seguro.SeguroId, "POL-B")

        Dim resultado = Await _service.GetList(Function(p) p.NumeroPoliza = "POL-B")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.IsNotNull(resultado.Value(0).Paciente)
        Assert.IsNotNull(resultado.Value(0).Seguro)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_RegistroExistente_LoQuitaYConservaLosDemas() As Task
        Dim seguro = SembrarSeguro(Factory)
        Dim a = SembrarPacienteSeguro(Factory, SembrarPaciente(Factory).PacienteId, seguro.SeguroId)
        Dim b = SembrarPacienteSeguro(Factory, SembrarPaciente(Factory).PacienteId, seguro.SeguroId)

        Dim resultado = Await _service.Eliminar(a.PacienteSeguroId)

        Assert.IsTrue(resultado.IsSuccess)
        Dim quedan = LeerTodos(Of PacienteSeguro)()
        Assert.AreEqual(1, quedan.Count)
        Assert.AreEqual(b.PacienteSeguroId, quedan(0).PacienteSeguroId)
    End Function

    ' Borrar algo que no existe debe reportar fallo, no éxito.
    <TestMethod>
    Public Async Function Eliminar_IdInexistente_RetornaFalloYNoAfectaOtrosRegistros() As Task
        Dim seguro = SembrarSeguro(Factory)
        SembrarPacienteSeguro(Factory, SembrarPaciente(Factory).PacienteId, seguro.SeguroId)

        Dim resultado = Await _service.Eliminar(999)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of PacienteSeguro)())
    End Function

#End Region

End Class
