Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class PagosServiceTests
    Inherits PruebaBase

    Private _service As PagosServices

    <TestInitialize>
    Public Sub Iniciar()
        _service = New PagosServices(Factory)
    End Sub

    Private Function NuevaCita() As Cita
        Return SembrarCita(Factory, SembrarPaciente(Factory).PacienteId)
    End Function

#Region "Guardar - pago nuevo"

    <TestMethod>
    Public Async Function Guardar_PagoDeCita_LoInsertaRecortandoEspacios() As Task
        Dim cita = NuevaCita()
        Dim pago As New Pago With {
            .CitaId = cita.CitaId, .Monto = 2500D, .MetodoPago = " Tarjeta ", .Concepto = " Abono consulta "}

        Dim resultado = Await _service.Guardar(pago)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Pago)()
        Assert.AreEqual(1, guardados.Count)
        Assert.IsTrue(guardados(0).PagoId > 0, "La base debe generar el PagoId.")
        Assert.AreEqual(cita.CitaId, guardados(0).CitaId)
        Assert.IsNull(guardados(0).CirugiaId)
        Assert.AreEqual(2500D, guardados(0).Monto)
        Assert.AreEqual("Tarjeta", guardados(0).MetodoPago)
        Assert.AreEqual("Abono consulta", guardados(0).Concepto)
        Assert.IsTrue(guardados(0).Fecha.Year >= 2026, "La fecha la pone el servidor.")
    End Function

    <TestMethod>
    Public Async Function Guardar_PagoDeCirugia_LoInsertaConLaCirugia() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)

        Dim resultado = Await _service.Guardar(New Pago With {.CirugiaId = cirugia.CirugiaId, .Monto = 10000D})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardado = LeerTodos(Of Pago)()(0)
        Assert.AreEqual(cirugia.CirugiaId, guardado.CirugiaId)
        Assert.IsNull(guardado.CitaId)
        Assert.IsNull(guardado.MetodoPago)
        Assert.IsNull(guardado.Concepto)
    End Function

    <TestMethod>
    Public Async Function Guardar_Nulo_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(Nothing))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function Guardar_MontoCeroONegativo_RetornaFallo() As Task
        Dim cita = NuevaCita()

        Dim cero = Await _service.Guardar(New Pago With {.CitaId = cita.CitaId, .Monto = 0D})
        Dim negativo = Await _service.Guardar(New Pago With {.CitaId = cita.CitaId, .Monto = -50D})

        Assert.IsFalse(cero.IsSuccess)
        Assert.IsFalse(negativo.IsSuccess)
        Assert.AreEqual(0, Contar(Of Pago)())
    End Function

    <TestMethod>
    Public Async Function Guardar_SinCirugiaNiCita_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New Pago With {.Monto = 100D})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Pago)())
    End Function

    ' Un pago es de una cirugía O de una cita, nunca de las dos.
    <TestMethod>
    Public Async Function Guardar_ConCirugiaYCita_RetornaFallo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim cita = NuevaCita()

        Dim resultado = Await _service.Guardar(New Pago With {
            .CirugiaId = cirugia.CirugiaId, .CitaId = cita.CitaId, .Monto = 100D})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Pago)())
    End Function

    <TestMethod>
    Public Async Function Guardar_CirugiaInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(New Pago With {.CirugiaId = 999, .Monto = 100D}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Pago)())
    End Function

    <TestMethod>
    Public Async Function Guardar_CitaInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(New Pago With {.CitaId = 999, .Monto = 100D}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Pago)())
    End Function

    <TestMethod>
    Public Async Function Guardar_TextosDe251Caracteres_RetornaFallo() As Task
        Dim cita = NuevaCita()
        Dim largo = New String("a"c, 251)

        Dim enMetodo = Await _service.Guardar(New Pago With {.CitaId = cita.CitaId, .Monto = 100D, .MetodoPago = largo})
        Dim enConcepto = Await _service.Guardar(New Pago With {.CitaId = cita.CitaId, .Monto = 100D, .Concepto = largo})

        Assert.IsFalse(enMetodo.IsSuccess)
        Assert.IsFalse(enConcepto.IsSuccess)
        Assert.AreEqual(0, Contar(Of Pago)())
    End Function

    <TestMethod>
    Public Async Function Guardar_TextosDe250Caracteres_SeAceptan() As Task
        Dim cita = NuevaCita()
        Dim limite = New String("a"c, 250)

        Dim resultado = Await _service.Guardar(New Pago With {
            .CitaId = cita.CitaId, .Monto = 100D, .MetodoPago = limite, .Concepto = limite})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Pago)())
    End Function

    ' La fecha del pago la pone el servidor: lo que mande el cliente se ignora.
    <TestMethod>
    Public Async Function Guardar_PagoNuevo_IgnoraLaFechaQueMandeElCliente() As Task
        Dim cita = NuevaCita()

        Await _service.Guardar(New Pago With {.CitaId = cita.CitaId, .Monto = 100D, .Fecha = FechaBase})

        Assert.IsTrue(LeerTodos(Of Pago)()(0).Fecha > FechaBase)
    End Function

#End Region

#Region "Guardar - pago existente"

    <TestMethod>
    Public Async Function Guardar_PagoExistente_ActualizaLosDatosSinDuplicar() As Task
        Dim cita = NuevaCita()
        Dim original = SembrarPago(Factory, citaId:=cita.CitaId, monto:=1000D)
        Dim modificado As New Pago With {
            .PagoId = original.PagoId, .CitaId = cita.CitaId, .Monto = 1500D,
            .MetodoPago = " Transferencia ", .Concepto = " Saldo "}

        Dim resultado = Await _service.Guardar(modificado)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Pago)()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual(1500D, guardados(0).Monto)
        Assert.AreEqual("Transferencia", guardados(0).MetodoPago)
        Assert.AreEqual("Saldo", guardados(0).Concepto)
    End Function

    <TestMethod>
    Public Async Function Guardar_PagoExistente_ConservaLaFechaOriginal() As Task
        Dim cita = NuevaCita()
        Dim original = SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase)

        Await _service.Guardar(New Pago With {
            .PagoId = original.PagoId, .CitaId = cita.CitaId, .Monto = 1500D, .Fecha = FechaBase.AddYears(1)})

        Assert.AreEqual(FechaBase, LeerTodos(Of Pago)()(0).Fecha)
    End Function

    ' Guardar con un ID que no existe no debe intentar insertarlo.
    <TestMethod>
    Public Async Function Guardar_PagoIdInexistente_RetornaFallo() As Task
        Dim cita = NuevaCita()

        Dim intento = Await Intentar(Function() _service.Guardar(New Pago With {
            .PagoId = 999, .CitaId = cita.CitaId, .Monto = 100D}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Pago)())
    End Function

    ' Regla actual: un pago puede reasignarse de una cita a una cirugía (y viceversa).
    <TestMethod>
    Public Async Function Guardar_PagoExistente_PuedeCambiarDeCitaACirugia() As Task
        Dim cita = NuevaCita()
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim original = SembrarPago(Factory, citaId:=cita.CitaId)

        Dim resultado = Await _service.Guardar(New Pago With {
            .PagoId = original.PagoId, .CirugiaId = cirugia.CirugiaId, .Monto = 1000D})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardado = LeerTodos(Of Pago)()(0)
        Assert.AreEqual(cirugia.CirugiaId, guardado.CirugiaId)
        Assert.IsNull(guardado.CitaId)
    End Function

    <TestMethod>
    Public Async Function Guardar_PagoExistente_ConMontoInvalido_RetornaFalloYNoModifica() As Task
        Dim cita = NuevaCita()
        Dim original = SembrarPago(Factory, citaId:=cita.CitaId, monto:=1000D)

        Dim resultado = Await _service.Guardar(New Pago With {
            .PagoId = original.PagoId, .CitaId = cita.CitaId, .Monto = 0D})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1000D, LeerTodos(Of Pago)()(0).Monto)
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_PagoDeCita_IncluyeLaCita() As Task
        Dim cita = NuevaCita()
        Dim pago = SembrarPago(Factory, citaId:=cita.CitaId)

        Dim resultado = Await _service.Buscar(pago.PagoId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.IsNotNull(resultado.Value.Cita)
        Assert.IsNull(resultado.Value.Cirugia)
    End Function

    <TestMethod>
    Public Async Function Buscar_PagoDeCirugia_IncluyeLaCirugia() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim pago = SembrarPago(Factory, cirugiaId:=cirugia.CirugiaId)

        Dim resultado = Await _service.Buscar(pago.PagoId)

        Assert.IsNotNull(resultado.Value.Cirugia)
        Assert.IsNull(resultado.Value.Cita)
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
    Public Async Function BuscarPorCirugia_RetornaSoloEsaCirugiaDelMasRecienteAlMasAntiguo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim otra = SembrarCirugiaCompleta(Factory)
        Dim antiguo = SembrarPago(Factory, cirugiaId:=cirugia.CirugiaId, fecha:=FechaBase)
        Dim reciente = SembrarPago(Factory, cirugiaId:=cirugia.CirugiaId, fecha:=FechaBase.AddDays(3))
        SembrarPago(Factory, cirugiaId:=otra.CirugiaId)

        Dim resultado = Await _service.BuscarPorCirugia(cirugia.CirugiaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual(reciente.PagoId, resultado.Value(0).PagoId)
        Assert.AreEqual(antiguo.PagoId, resultado.Value(1).PagoId)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCirugia_SinPagos_RetornaListaVacia() As Task
        Dim resultado = Await _service.BuscarPorCirugia(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCita_RetornaSoloEsaCitaDelMasRecienteAlMasAntiguo() As Task
        Dim cita = NuevaCita()
        Dim otra = NuevaCita()
        Dim antiguo = SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase)
        Dim reciente = SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase.AddDays(3))
        SembrarPago(Factory, citaId:=otra.CitaId)

        Dim resultado = Await _service.BuscarPorCita(cita.CitaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual(reciente.PagoId, resultado.Value(0).PagoId)
        Assert.AreEqual(antiguo.PagoId, resultado.Value(1).PagoId)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCita_SinPagos_RetornaListaVacia() As Task
        Dim resultado = Await _service.BuscarPorCita(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    ' Busca por el día completo, sin importar la hora que se le pase.
    <TestMethod>
    Public Async Function BuscarPorFecha_RetornaSoloLosPagosDeEseDia() As Task
        Dim cita = NuevaCita()
        Dim ayer = SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase.AddDays(-1))
        Dim manana = SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase.AddDays(1))
        Dim temprano = SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase.Date.AddHours(8))
        Dim tarde = SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase.Date.AddHours(20))

        Dim resultado = Await _service.BuscarPorFecha(FechaBase.AddHours(3))

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual(tarde.PagoId, resultado.Value(0).PagoId, "El más reciente primero.")
        Assert.AreEqual(temprano.PagoId, resultado.Value(1).PagoId)
    End Function

    <TestMethod>
    Public Async Function BuscarPorFecha_SinPagos_RetornaListaVacia() As Task
        Dim cita = NuevaCita()
        SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase)

        Dim resultado = Await _service.BuscarPorFecha(FechaBase.AddDays(30))

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    ' El rango incluye el día inicial y el día final completos.
    <TestMethod>
    Public Async Function BuscarPorRangoFechas_IncluyeAmbosExtremosYExcluyeLoDemas() As Task
        Dim cita = NuevaCita()
        SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase.AddDays(-1))
        Dim inicio = SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase.Date)
        Dim fin = SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase.Date.AddDays(2).AddHours(23))
        SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase.AddDays(3))

        Dim resultado = Await _service.BuscarPorRangoFechas(FechaBase, FechaBase.AddDays(2))

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual(fin.PagoId, resultado.Value(0).PagoId)
        Assert.AreEqual(inicio.PagoId, resultado.Value(1).PagoId)
    End Function

    ' Rango al revés: no es un error, simplemente no hay resultados.
    <TestMethod>
    Public Async Function BuscarPorRangoFechas_InicioPosteriorAlFin_RetornaListaVacia() As Task
        Dim cita = NuevaCita()
        SembrarPago(Factory, citaId:=cita.CitaId, fecha:=FechaBase)

        Dim resultado = Await _service.BuscarPorRangoFechas(FechaBase.AddDays(5), FechaBase)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorMetodoDePago() As Task
        Dim cita = NuevaCita()
        SembrarPago(Factory, citaId:=cita.CitaId, metodo:="Efectivo")
        SembrarPago(Factory, citaId:=cita.CitaId, metodo:="Tarjeta")

        Dim resultado = Await _service.GetList(Function(p) p.MetodoPago = "Tarjeta")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.IsNotNull(resultado.Value(0).Cita)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_PagoExistente_LoQuitaYConservaLosDemas() As Task
        Dim cita = NuevaCita()
        Dim a = SembrarPago(Factory, citaId:=cita.CitaId)
        Dim b = SembrarPago(Factory, citaId:=cita.CitaId)

        Dim resultado = Await _service.Eliminar(a.PagoId)

        Assert.IsTrue(resultado.IsSuccess)
        Dim quedan = LeerTodos(Of Pago)()
        Assert.AreEqual(1, quedan.Count)
        Assert.AreEqual(b.PagoId, quedan(0).PagoId)
    End Function

    ' Borrar algo que no existe debe reportar fallo, no éxito.
    <TestMethod>
    Public Async Function Eliminar_IdInexistente_RetornaFalloYNoAfectaOtrosRegistros() As Task
        Dim cita = NuevaCita()
        SembrarPago(Factory, citaId:=cita.CitaId)

        Dim resultado = Await _service.Eliminar(999)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Pago)())
    End Function

#End Region

End Class
