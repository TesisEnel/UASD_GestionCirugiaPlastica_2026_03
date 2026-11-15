Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class PreoperatoriosServiceTests
    Inherits PruebaBase

    Private _service As PreoperatoriosServices

    <TestInitialize>
    Public Sub Iniciar()
        _service = New PreoperatoriosServices(Factory)
    End Sub

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_EvaluacionAutorizadaCompleta_LaInsertaRecortandoEspacios() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim evaluacion As New Preoperatorio With {
            .CirugiaId = cirugia.CirugiaId, .Autorizado = True,
            .AnalisisLaboratorio = " Hemograma normal ", .EvaluacionCardiologica = " Sin hallazgos ",
            .EvaluacionAnestesica = " ASA I ", .Observaciones = " Apto para cirugia "}

        Dim resultado = Await _service.Guardar(evaluacion)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Preoperatorio)()
        Assert.AreEqual(1, guardados.Count)
        Assert.IsTrue(guardados(0).PreoperatorioId > 0, "La base debe generar el Id.")
        Assert.AreEqual(cirugia.CirugiaId, guardados(0).CirugiaId)
        Assert.IsTrue(guardados(0).Autorizado)
        Assert.AreEqual("Hemograma normal", guardados(0).AnalisisLaboratorio)
        Assert.AreEqual("Sin hallazgos", guardados(0).EvaluacionCardiologica)
        Assert.AreEqual("ASA I", guardados(0).EvaluacionAnestesica)
        Assert.AreEqual("Apto para cirugia", guardados(0).Observaciones)
        Assert.IsTrue(guardados(0).FechaEvaluacion.Year >= 2026, "La fecha la pone el servidor.")
    End Function

    ' Una evaluación sin autorizar puede registrarse incompleta (está pendiente).
    <TestMethod>
    Public Async Function Guardar_NoAutorizadaSinDatosClinicos_SeGuardaConNulos() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)

        Dim resultado = Await _service.Guardar(New Preoperatorio With {.CirugiaId = cirugia.CirugiaId})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardado = LeerTodos(Of Preoperatorio)()(0)
        Assert.IsFalse(guardado.Autorizado)
        Assert.IsNull(guardado.AnalisisLaboratorio)
        Assert.IsNull(guardado.EvaluacionCardiologica)
        Assert.IsNull(guardado.EvaluacionAnestesica)
    End Function

    <TestMethod>
    Public Async Function Guardar_Nulo_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(Nothing))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function Guardar_SinCirugia_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New Preoperatorio With {.Observaciones = "Sin cirugia"})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Preoperatorio)())
    End Function

    <TestMethod>
    Public Async Function Guardar_CirugiaInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(New Preoperatorio With {.CirugiaId = 999}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Preoperatorio)())
    End Function

    ' Para autorizar se exigen los tres requisitos; basta que falte uno (o venga solo con espacios).
    <TestMethod>
    Public Async Function Guardar_AutorizadaFaltandoUnRequisito_RetornaFallo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)

        Dim sinLaboratorio = Await _service.Guardar(New Preoperatorio With {
            .CirugiaId = cirugia.CirugiaId, .Autorizado = True,
            .EvaluacionCardiologica = "Normal", .EvaluacionAnestesica = "ASA I"})
        Dim sinCardiologia = Await _service.Guardar(New Preoperatorio With {
            .CirugiaId = cirugia.CirugiaId, .Autorizado = True,
            .AnalisisLaboratorio = "Normal", .EvaluacionAnestesica = "ASA I"})
        Dim sinAnestesia = Await _service.Guardar(New Preoperatorio With {
            .CirugiaId = cirugia.CirugiaId, .Autorizado = True,
            .AnalisisLaboratorio = "Normal", .EvaluacionCardiologica = "Normal"})
        Dim conEspacios = Await _service.Guardar(New Preoperatorio With {
            .CirugiaId = cirugia.CirugiaId, .Autorizado = True,
            .AnalisisLaboratorio = "   ", .EvaluacionCardiologica = "Normal", .EvaluacionAnestesica = "ASA I"})

        Assert.IsFalse(sinLaboratorio.IsSuccess)
        Assert.IsFalse(sinCardiologia.IsSuccess)
        Assert.IsFalse(sinAnestesia.IsSuccess)
        Assert.IsFalse(conEspacios.IsSuccess)
        Assert.AreEqual(0, Contar(Of Preoperatorio)())
    End Function

    ' Las evaluaciones clínicas no se editan: cada cambio es un registro nuevo.
    <TestMethod>
    Public Async Function Guardar_ConIdYaAsignado_RetornaFalloYNoModificaNada() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim original = SembrarPreoperatorio(Factory, cirugia.CirugiaId)

        Dim resultado = Await _service.Guardar(New Preoperatorio With {
            .PreoperatorioId = original.PreoperatorioId, .CirugiaId = cirugia.CirugiaId,
            .Observaciones = "Cambiada"})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreNotEqual("Cambiada", LeerTodos(Of Preoperatorio)()(0).Observaciones)
    End Function

    ' La fecha de la evaluación la pone el servidor: lo que mande el cliente se ignora.
    <TestMethod>
    Public Async Function Guardar_IgnoraLaFechaQueMandeElCliente() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)

        Await _service.Guardar(New Preoperatorio With {.CirugiaId = cirugia.CirugiaId, .FechaEvaluacion = FechaBase})

        Assert.IsTrue(LeerTodos(Of Preoperatorio)()(0).FechaEvaluacion > FechaBase)
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_RetornaLaEvaluacion() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim evaluacion = SembrarPreoperatorio(Factory, cirugia.CirugiaId)

        Dim resultado = Await _service.Buscar(evaluacion.PreoperatorioId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual(cirugia.CirugiaId, resultado.Value.CirugiaId)
    End Function

    <TestMethod>
    Public Async Function Buscar_IdInexistente_RetornaValorNulo() As Task
        Dim resultado = Await _service.Buscar(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNull(resultado.Value)
    End Function

#End Region

#Region "Historial y busquedas"

    <TestMethod>
    Public Async Function BuscarHistorial_RetornaSoloEsaCirugiaDeLaMasRecienteALaMasAntigua() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim otra = SembrarCirugiaCompleta(Factory)
        Dim primera = SembrarPreoperatorio(Factory, cirugia.CirugiaId, fecha:=FechaBase)
        Dim tercera = SembrarPreoperatorio(Factory, cirugia.CirugiaId, fecha:=FechaBase.AddDays(10))
        Dim segunda = SembrarPreoperatorio(Factory, cirugia.CirugiaId, fecha:=FechaBase.AddDays(5))
        SembrarPreoperatorio(Factory, otra.CirugiaId)

        Dim resultado = Await _service.BuscarHistorial(cirugia.CirugiaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(3, resultado.Value.Count)
        Assert.AreEqual(tercera.PreoperatorioId, resultado.Value(0).PreoperatorioId)
        Assert.AreEqual(segunda.PreoperatorioId, resultado.Value(1).PreoperatorioId)
        Assert.AreEqual(primera.PreoperatorioId, resultado.Value(2).PreoperatorioId)
    End Function

    <TestMethod>
    Public Async Function BuscarHistorial_MismaFecha_DesempataPorIdDescendente() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim a = SembrarPreoperatorio(Factory, cirugia.CirugiaId, fecha:=FechaBase)
        Dim b = SembrarPreoperatorio(Factory, cirugia.CirugiaId, fecha:=FechaBase)

        Dim resultado = Await _service.BuscarHistorial(cirugia.CirugiaId)

        Assert.AreEqual(b.PreoperatorioId, resultado.Value(0).PreoperatorioId)
        Assert.AreEqual(a.PreoperatorioId, resultado.Value(1).PreoperatorioId)
    End Function

    <TestMethod>
    Public Async Function BuscarHistorial_SinEvaluaciones_RetornaListaVacia() As Task
        Dim resultado = Await _service.BuscarHistorial(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCirugia_RetornaLoMismoQueElHistorial() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim antigua = SembrarPreoperatorio(Factory, cirugia.CirugiaId, fecha:=FechaBase)
        Dim reciente = SembrarPreoperatorio(Factory, cirugia.CirugiaId, fecha:=FechaBase.AddDays(2))
        SembrarPreoperatorio(Factory, SembrarCirugiaCompleta(Factory).CirugiaId)

        Dim resultado = Await _service.BuscarPorCirugia(cirugia.CirugiaId)

        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual(reciente.PreoperatorioId, resultado.Value(0).PreoperatorioId)
        Assert.AreEqual(antigua.PreoperatorioId, resultado.Value(1).PreoperatorioId)
    End Function

    <TestMethod>
    Public Async Function BuscarPorAutorizacion_FiltraSegunElValorPedido() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim autorizada = SembrarPreoperatorio(Factory, cirugia.CirugiaId, autorizado:=True)
        Dim noAutorizada = SembrarPreoperatorio(Factory, cirugia.CirugiaId, autorizado:=False)

        Dim siAutorizadas = Await _service.BuscarPorAutorizacion(True)
        Dim noAutorizadas = Await _service.BuscarPorAutorizacion(False)

        Assert.AreEqual(1, siAutorizadas.Value.Count)
        Assert.AreEqual(autorizada.PreoperatorioId, siAutorizadas.Value(0).PreoperatorioId)
        Assert.AreEqual(1, noAutorizadas.Value.Count)
        Assert.AreEqual(noAutorizada.PreoperatorioId, noAutorizadas.Value(0).PreoperatorioId)
    End Function

    ' Pendiente = sin autorizar, o con algún requisito nulo o vacío.
    <TestMethod>
    Public Async Function BuscarRequisitosPendientes_RetornaSoloLasQueTienenAlgoPendiente() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory).CirugiaId
        SembrarPreoperatorio(Factory, cirugia, autorizado:=True, fecha:=FechaBase)
        Dim sinAutorizar = SembrarPreoperatorio(Factory, cirugia, autorizado:=False, fecha:=FechaBase.AddDays(1))
        Dim laboratorioVacio = SembrarPreoperatorio(Factory, cirugia, autorizado:=True, fecha:=FechaBase.AddDays(2), laboratorio:="   ")
        Dim cardiologiaNula = SembrarPreoperatorio(Factory, cirugia, autorizado:=True, fecha:=FechaBase.AddDays(3), cardiologica:=Nothing)
        Dim anestesiaNula = SembrarPreoperatorio(Factory, cirugia, autorizado:=True, fecha:=FechaBase.AddDays(4), anestesica:=Nothing)

        Dim resultado = Await _service.BuscarRequisitosPendientes()

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(4, resultado.Value.Count, "La evaluación completa y autorizada no es pendiente.")
        Assert.AreEqual(anestesiaNula.PreoperatorioId, resultado.Value(0).PreoperatorioId)
        Assert.AreEqual(cardiologiaNula.PreoperatorioId, resultado.Value(1).PreoperatorioId)
        Assert.AreEqual(laboratorioVacio.PreoperatorioId, resultado.Value(2).PreoperatorioId)
        Assert.AreEqual(sinAutorizar.PreoperatorioId, resultado.Value(3).PreoperatorioId)
    End Function

    <TestMethod>
    Public Async Function BuscarRequisitosPendientes_TodasCompletas_RetornaListaVacia() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        SembrarPreoperatorio(Factory, cirugia.CirugiaId, autorizado:=True)

        Dim resultado = Await _service.BuscarRequisitosPendientes()

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    ' El rango incluye el día inicial y el día final completos.
    <TestMethod>
    Public Async Function BuscarPorFecha_IncluyeAmbosExtremosYExcluyeLoDemas() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory).CirugiaId
        SembrarPreoperatorio(Factory, cirugia, fecha:=FechaBase.AddDays(-1))
        Dim inicio = SembrarPreoperatorio(Factory, cirugia, fecha:=FechaBase.Date)
        Dim fin = SembrarPreoperatorio(Factory, cirugia, fecha:=FechaBase.Date.AddDays(2).AddHours(23))
        SembrarPreoperatorio(Factory, cirugia, fecha:=FechaBase.AddDays(3))

        Dim resultado = Await _service.BuscarPorFecha(FechaBase, FechaBase.AddDays(2))

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual(fin.PreoperatorioId, resultado.Value(0).PreoperatorioId)
        Assert.AreEqual(inicio.PreoperatorioId, resultado.Value(1).PreoperatorioId)
    End Function

    ' Rango al revés: no es un error, simplemente no hay resultados.
    <TestMethod>
    Public Async Function BuscarPorFecha_DesdePosteriorAHasta_RetornaListaVacia() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        SembrarPreoperatorio(Factory, cirugia.CirugiaId, fecha:=FechaBase)

        Dim resultado = Await _service.BuscarPorFecha(FechaBase.AddDays(5), FechaBase)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorObservaciones() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        SembrarPreoperatorio(Factory, cirugia.CirugiaId, observaciones:="Control A")
        SembrarPreoperatorio(Factory, cirugia.CirugiaId, observaciones:="Control B")

        Dim resultado = Await _service.GetList(Function(p) p.Observaciones = "Control B")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
    End Function

#End Region

#Region "Eliminar"

    ' Las evaluaciones clínicas no se borran: debe avisar con un fallo (no con excepción).
    <TestMethod>
    Public Async Function Eliminar_NoSePermite_RetornaFalloYNoBorraNada() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim evaluacion = SembrarPreoperatorio(Factory, cirugia.CirugiaId)

        Dim intento = Await Intentar(Function() _service.Eliminar(evaluacion.PreoperatorioId))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Preoperatorio)())
    End Function

#End Region

End Class
