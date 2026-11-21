Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class RecetasServiceTests
    Inherits PruebaBase

    Private _service As RecetasServices

    <TestInitialize>
    Public Sub Iniciar()
        _service = New RecetasServices(Factory)
    End Sub

    ' Lista de detalles con solo el nombre del medicamento.
    Private Shared Function Detalles(ParamArray medicamentos As String()) As List(Of DetallesReceta)
        Return medicamentos.Select(Function(m) New DetallesReceta With {.Medicamento = m}).ToList()
    End Function

#Region "Guardar - receta nueva"

    <TestMethod>
    Public Async Function Guardar_RecetaValida_LaInsertaConSusDetallesRecortandoEspacios() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta As New Receta With {
            .PacienteId = paciente.PacienteId, .Indicaciones = " Tomar con comida ",
            .DetallesReceta = New List(Of DetallesReceta) From {
                New DetallesReceta With {.Medicamento = " Ibuprofeno ", .Dosis = " 400 mg ", .Frecuencia = " Cada 8 horas ",
                                         .Duracion = " 5 dias ", .Instrucciones = " Despues de comer "},
                New DetallesReceta With {.Medicamento = "Amoxicilina"}}}

        Dim resultado = Await _service.Guardar(receta)

        Assert.IsTrue(resultado.IsSuccess)
        Dim recetas = LeerTodos(Of Receta)()
        Assert.AreEqual(1, recetas.Count)
        Assert.IsTrue(recetas(0).RecetaId > 0, "La base debe generar el Id.")
        Assert.AreEqual(paciente.PacienteId, recetas(0).PacienteId)
        Assert.AreEqual("Tomar con comida", recetas(0).Indicaciones)
        Assert.IsTrue(recetas(0).Fecha.Year >= 2026, "La fecha la pone el servidor.")

        Dim detalles = LeerTodos(Of DetallesReceta)()
        Assert.AreEqual(2, detalles.Count)
        Dim ibuprofeno = detalles.Single(Function(d) d.Medicamento = "Ibuprofeno")
        Assert.AreEqual(recetas(0).RecetaId, ibuprofeno.RecetaId)
        Assert.AreEqual("400 mg", ibuprofeno.Dosis)
        Assert.AreEqual("Cada 8 horas", ibuprofeno.Frecuencia)
        Assert.AreEqual("5 dias", ibuprofeno.Duracion)
        Assert.AreEqual("Despues de comer", ibuprofeno.Instrucciones)
    End Function

    <TestMethod>
    Public Async Function Guardar_SoloPacienteYUnMedicamento_DejaLosOpcionalesNulos() As Task
        Dim paciente = SembrarPaciente(Factory)

        Dim resultado = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .DetallesReceta = Detalles("Ibuprofeno")})

        Assert.IsTrue(resultado.IsSuccess)
        Dim receta = LeerTodos(Of Receta)()(0)
        Assert.IsNull(receta.ConsultaId)
        Assert.IsNull(receta.CirugiaId)
        Assert.IsNull(receta.UsuarioId)
        Assert.IsNull(receta.Indicaciones)
        Dim detalle = LeerTodos(Of DetallesReceta)()(0)
        Assert.IsNull(detalle.Dosis)
        Assert.IsNull(detalle.Frecuencia)
    End Function

    <TestMethod>
    Public Async Function Guardar_ConConsultaDelPaciente_GuardaLaConsulta() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim consulta = SembrarConsulta(Factory, paciente.PacienteId)

        Dim resultado = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .ConsultaId = consulta.ConsultaId, .DetallesReceta = Detalles("Ibuprofeno")})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(consulta.ConsultaId, LeerTodos(Of Receta)()(0).ConsultaId)
    End Function

    <TestMethod>
    Public Async Function Guardar_ConCirugiaDelPaciente_GuardaLaCirugia() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)

        Dim resultado = Await _service.Guardar(New Receta With {
            .PacienteId = cirugia.PacienteId, .CirugiaId = cirugia.CirugiaId, .DetallesReceta = Detalles("Ibuprofeno")})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(cirugia.CirugiaId, LeerTodos(Of Receta)()(0).CirugiaId)
    End Function

    <TestMethod>
    Public Async Function Guardar_ConUsuarioExistente_GuardaElUsuario() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim usuario = SembrarUsuario(Factory)

        Dim resultado = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .UsuarioId = usuario.UsuarioId, .DetallesReceta = Detalles("Ibuprofeno")})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(usuario.UsuarioId, LeerTodos(Of Receta)()(0).UsuarioId)
    End Function

    <TestMethod>
    Public Async Function Guardar_Nula_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(Nothing))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function Guardar_SinPaciente_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New Receta With {.DetallesReceta = Detalles("Ibuprofeno")})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Receta)())
    End Function

    <TestMethod>
    Public Async Function Guardar_PacienteInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(New Receta With {
            .PacienteId = 999, .DetallesReceta = Detalles("Ibuprofeno")}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Receta)())
    End Function

    <TestMethod>
    Public Async Function Guardar_SinMedicamentos_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)

        Dim lista = Await _service.Guardar(New Receta With {.PacienteId = paciente.PacienteId})
        Dim nula = Await _service.Guardar(New Receta With {.PacienteId = paciente.PacienteId, .DetallesReceta = Nothing})

        Assert.IsFalse(lista.IsSuccess)
        Assert.IsFalse(nula.IsSuccess)
        Assert.AreEqual(0, Contar(Of Receta)())
    End Function

    ' Basta un medicamento sin nombre para que no se guarde nada de la receta.
    <TestMethod>
    Public Async Function Guardar_UnMedicamentoSinNombre_RetornaFalloYNoGuardaNada() As Task
        Dim paciente = SembrarPaciente(Factory)

        Dim vacio = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .DetallesReceta = Detalles("Ibuprofeno", "")})
        Dim espacios = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .DetallesReceta = Detalles("Ibuprofeno", "   ")})
        Dim nulo = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .DetallesReceta = Detalles("Ibuprofeno", Nothing)})

        Assert.IsFalse(vacio.IsSuccess)
        Assert.IsFalse(espacios.IsSuccess)
        Assert.IsFalse(nulo.IsSuccess)
        Assert.AreEqual(0, Contar(Of Receta)())
        Assert.AreEqual(0, Contar(Of DetallesReceta)())
    End Function

    <TestMethod>
    Public Async Function Guardar_TextosDelDetalleDe251Caracteres_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim largo = New String("a"c, 251)

        Dim enMedicamento = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .DetallesReceta = Detalles(largo)})
        Dim enDosis = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId,
            .DetallesReceta = New List(Of DetallesReceta) From {New DetallesReceta With {.Medicamento = "Ibuprofeno", .Dosis = largo}}})
        Dim enInstrucciones = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId,
            .DetallesReceta = New List(Of DetallesReceta) From {New DetallesReceta With {.Medicamento = "Ibuprofeno", .Instrucciones = largo}}})

        Assert.IsFalse(enMedicamento.IsSuccess)
        Assert.IsFalse(enDosis.IsSuccess)
        Assert.IsFalse(enInstrucciones.IsSuccess)
        Assert.AreEqual(0, Contar(Of Receta)())
    End Function

    <TestMethod>
    Public Async Function Guardar_IndicacionesDe4001Caracteres_RetornaFalloYDe4000SeAceptan() As Task
        Dim paciente = SembrarPaciente(Factory)

        Dim demasiado = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .Indicaciones = New String("a"c, 4001), .DetallesReceta = Detalles("Ibuprofeno")})
        Dim limite = Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .Indicaciones = New String("a"c, 4000), .DetallesReceta = Detalles("Ibuprofeno")})

        Assert.IsFalse(demasiado.IsSuccess)
        Assert.IsTrue(limite.IsSuccess)
        Assert.AreEqual(1, Contar(Of Receta)())
    End Function

    ' Una receta es de una consulta O de una cirugía, nunca de las dos.
    <TestMethod>
    Public Async Function Guardar_ConConsultaYCirugia_RetornaFallo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim consulta = SembrarConsulta(Factory, cirugia.PacienteId)

        Dim resultado = Await _service.Guardar(New Receta With {
            .PacienteId = cirugia.PacienteId, .ConsultaId = consulta.ConsultaId, .CirugiaId = cirugia.CirugiaId,
            .DetallesReceta = Detalles("Ibuprofeno")})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Receta)())
    End Function

    <TestMethod>
    Public Async Function Guardar_ConsultaDeOtroPacienteOInexistente_RetornaFallo() As Task
        Dim ana = SembrarPaciente(Factory)
        Dim luis = SembrarPaciente(Factory)
        Dim consultaDeLuis = SembrarConsulta(Factory, luis.PacienteId)

        Dim deOtro = Await _service.Guardar(New Receta With {
            .PacienteId = ana.PacienteId, .ConsultaId = consultaDeLuis.ConsultaId, .DetallesReceta = Detalles("Ibuprofeno")})
        Dim inexistente = Await _service.Guardar(New Receta With {
            .PacienteId = ana.PacienteId, .ConsultaId = 999, .DetallesReceta = Detalles("Ibuprofeno")})

        Assert.IsFalse(deOtro.IsSuccess)
        Assert.IsFalse(inexistente.IsSuccess)
        Assert.AreEqual(0, Contar(Of Receta)())
    End Function

    <TestMethod>
    Public Async Function Guardar_CirugiaDeOtroPacienteOInexistente_RetornaFallo() As Task
        Dim ana = SembrarPaciente(Factory)
        Dim cirugiaDeOtro = SembrarCirugiaCompleta(Factory)

        Dim deOtro = Await _service.Guardar(New Receta With {
            .PacienteId = ana.PacienteId, .CirugiaId = cirugiaDeOtro.CirugiaId, .DetallesReceta = Detalles("Ibuprofeno")})
        Dim inexistente = Await _service.Guardar(New Receta With {
            .PacienteId = ana.PacienteId, .CirugiaId = 999, .DetallesReceta = Detalles("Ibuprofeno")})

        Assert.IsFalse(deOtro.IsSuccess)
        Assert.IsFalse(inexistente.IsSuccess)
        Assert.AreEqual(0, Contar(Of Receta)())
    End Function

    <TestMethod>
    Public Async Function Guardar_UsuarioInexistente_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)

        Dim intento = Await Intentar(Function() _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .UsuarioId = 999, .DetallesReceta = Detalles("Ibuprofeno")}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Receta)())
    End Function

    ' En altas nuevas se conserva la fecha que llega en la solicitud.
    <TestMethod>
    Public Async Function Guardar_RecetaNueva_IgnoraLaFechaQueMandeElCliente() As Task
        Dim paciente = SembrarPaciente(Factory)

        Await _service.Guardar(New Receta With {
            .PacienteId = paciente.PacienteId, .Fecha = FechaBase, .DetallesReceta = Detalles("Ibuprofeno")})

        Assert.AreEqual(FechaBase, LeerTodos(Of Receta)()(0).Fecha)
    End Function

#End Region

#Region "Guardar - receta existente"

    <TestMethod>
    Public Async Function Guardar_RecetaExistente_ActualizaIndicacionesYMedicamentoSinDuplicar() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim original = SembrarRecetaDetallada(Factory, paciente.PacienteId)

        Dim resultado = Await _service.Guardar(New Receta With {
            .RecetaId = original.RecetaId, .PacienteId = paciente.PacienteId, .Indicaciones = " Nuevas indicaciones ",
            .DetallesReceta = New List(Of DetallesReceta) From {
                New DetallesReceta With {
                    .DetalleRecetaId = original.DetallesReceta.First().DetalleRecetaId,
                    .Medicamento = " Naproxeno ", .Dosis = " 500 mg "}}})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Receta)())
        Assert.AreEqual("Nuevas indicaciones", LeerTodos(Of Receta)()(0).Indicaciones)
        Dim detalles = LeerTodos(Of DetallesReceta)()
        Assert.AreEqual(1, detalles.Count)
        Assert.AreEqual("Naproxeno", detalles(0).Medicamento)
        Assert.AreEqual("500 mg", detalles(0).Dosis)
    End Function

    ' Los medicamentos que no vienen en la solicitud se conservan (son historia clínica).
    <TestMethod>
    Public Async Function Guardar_RecetaExistente_AgregaMedicamentoNuevoYConservaLosAnteriores() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim original = SembrarRecetaDetallada(Factory, paciente.PacienteId, medicamento:="Ibuprofeno")

        Dim resultado = Await _service.Guardar(New Receta With {
            .RecetaId = original.RecetaId, .PacienteId = paciente.PacienteId, .DetallesReceta = Detalles("Amoxicilina")})

        Assert.IsTrue(resultado.IsSuccess)
        Dim medicamentos = LeerTodos(Of DetallesReceta)().Select(Function(d) d.Medicamento).OrderBy(Function(m) m).ToList()
        Assert.AreEqual(2, medicamentos.Count)
        Assert.AreEqual("Amoxicilina", medicamentos(0))
        Assert.AreEqual("Ibuprofeno", medicamentos(1))
    End Function

    <TestMethod>
    Public Async Function Guardar_RecetaExistente_ActualizaFechaYConservaUsuario() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim usuario = SembrarUsuario(Factory)
        Dim otro = SembrarUsuario(Factory)
        Dim original = SembrarRecetaDetallada(Factory, paciente.PacienteId, usuarioId:=usuario.UsuarioId, fecha:=FechaBase)

        Await _service.Guardar(New Receta With {
            .RecetaId = original.RecetaId, .PacienteId = paciente.PacienteId, .UsuarioId = otro.UsuarioId,
            .Fecha = FechaBase.AddYears(1), .DetallesReceta = Detalles("Amoxicilina")})

        Dim guardada = LeerTodos(Of Receta)()(0)
        Assert.AreEqual(FechaBase.AddYears(1), guardada.Fecha)
        Assert.AreEqual(usuario.UsuarioId, guardada.UsuarioId.Value, "Quien emitió la receta no cambia al editarla.")
    End Function

    ' Guardar con un ID que no existe no debe intentar insertarla.
    <TestMethod>
    Public Async Function Guardar_RecetaIdInexistente_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)

        Dim intento = Await Intentar(Function() _service.Guardar(New Receta With {
            .RecetaId = 999, .PacienteId = paciente.PacienteId, .DetallesReceta = Detalles("Ibuprofeno")}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Receta)())
    End Function

    <TestMethod>
    Public Async Function Guardar_CambiarDePaciente_RetornaFallo() As Task
        Dim ana = SembrarPaciente(Factory)
        Dim luis = SembrarPaciente(Factory)
        Dim original = SembrarRecetaDetallada(Factory, ana.PacienteId)

        Dim resultado = Await _service.Guardar(New Receta With {
            .RecetaId = original.RecetaId, .PacienteId = luis.PacienteId, .DetallesReceta = Detalles("Amoxicilina")})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(ana.PacienteId, LeerTodos(Of Receta)()(0).PacienteId)
        Assert.AreEqual(1, Contar(Of DetallesReceta)())
    End Function

    <TestMethod>
    Public Async Function Guardar_CambiarElOrigen_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim consulta = SembrarConsulta(Factory, paciente.PacienteId)
        Dim original = SembrarRecetaDetallada(Factory, paciente.PacienteId)

        Dim resultado = Await _service.Guardar(New Receta With {
            .RecetaId = original.RecetaId, .PacienteId = paciente.PacienteId, .ConsultaId = consulta.ConsultaId,
            .DetallesReceta = Detalles("Amoxicilina")})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.IsNull(LeerTodos(Of Receta)()(0).ConsultaId)
    End Function

    ' Si un medicamento no pertenece a esta receta, no se aplica ningún cambio.
    <TestMethod>
    Public Async Function Guardar_MedicamentoDeOtraRecetaOInexistente_RetornaFalloYNoCambiaNada() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta1 = SembrarRecetaDetallada(Factory, paciente.PacienteId, medicamento:="Ibuprofeno", indicaciones:="Originales")
        Dim receta2 = SembrarRecetaDetallada(Factory, paciente.PacienteId, medicamento:="Paracetamol")

        Dim deOtra = Await _service.Guardar(New Receta With {
            .RecetaId = receta1.RecetaId, .PacienteId = paciente.PacienteId, .Indicaciones = "Cambiadas",
            .DetallesReceta = New List(Of DetallesReceta) From {
                New DetallesReceta With {.DetalleRecetaId = receta2.DetallesReceta.First().DetalleRecetaId, .Medicamento = "Hackeado"}}})
        Dim inexistente = Await _service.Guardar(New Receta With {
            .RecetaId = receta1.RecetaId, .PacienteId = paciente.PacienteId, .Indicaciones = "Cambiadas",
            .DetallesReceta = New List(Of DetallesReceta) From {
                New DetallesReceta With {.DetalleRecetaId = 999, .Medicamento = "Fantasma"}}})

        Assert.IsFalse(deOtra.IsSuccess)
        Assert.IsFalse(inexistente.IsSuccess)
        Assert.AreEqual("Originales", LeerTodos(Of Receta)().Single(Function(r) r.RecetaId = receta1.RecetaId).Indicaciones)
        Dim medicamentos = LeerTodos(Of DetallesReceta)().Select(Function(d) d.Medicamento).OrderBy(Function(m) m).ToList()
        Assert.AreEqual(2, medicamentos.Count)
        Assert.AreEqual("Ibuprofeno", medicamentos(0))
        Assert.AreEqual("Paracetamol", medicamentos(1))
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyeSusMedicamentos() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarRecetaDetallada(Factory, paciente.PacienteId, medicamento:="Ibuprofeno")

        Dim resultado = Await _service.Buscar(receta.RecetaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual(1, resultado.Value.DetallesReceta.Count)
        Assert.AreEqual("Ibuprofeno", resultado.Value.DetallesReceta.First().Medicamento)
    End Function

    <TestMethod>
    Public Async Function Buscar_IdInexistente_RetornaValorNulo() As Task
        Dim resultado = Await _service.Buscar(999)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNull(resultado.Value)
    End Function

#End Region

#Region "Listados y busquedas"

    <TestMethod>
    Public Async Function BuscarPorPaciente_RetornaSoloEsePacienteDelMasRecienteAlMasAntiguo() As Task
        Dim ana = SembrarPaciente(Factory)
        Dim luis = SembrarPaciente(Factory)
        Dim primera = SembrarRecetaDetallada(Factory, ana.PacienteId, fecha:=FechaBase)
        Dim tercera = SembrarRecetaDetallada(Factory, ana.PacienteId, fecha:=FechaBase.AddDays(2))
        Dim segunda = SembrarRecetaDetallada(Factory, ana.PacienteId, fecha:=FechaBase.AddDays(1))
        SembrarRecetaDetallada(Factory, luis.PacienteId)

        Dim resultado = Await _service.BuscarPorPaciente(ana.PacienteId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(3, resultado.Value.Count)
        Assert.AreEqual(tercera.RecetaId, resultado.Value(0).RecetaId)
        Assert.AreEqual(segunda.RecetaId, resultado.Value(1).RecetaId)
        Assert.AreEqual(primera.RecetaId, resultado.Value(2).RecetaId)
        Assert.AreEqual(1, resultado.Value(0).DetallesReceta.Count, "Debe incluir los medicamentos.")
    End Function

    <TestMethod>
    Public Async Function BuscarPorPaciente_MismaFecha_DesempataPorIdDescendente() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim a = SembrarRecetaDetallada(Factory, paciente.PacienteId, fecha:=FechaBase)
        Dim b = SembrarRecetaDetallada(Factory, paciente.PacienteId, fecha:=FechaBase)

        Dim resultado = Await _service.BuscarPorPaciente(paciente.PacienteId)

        Assert.AreEqual(b.RecetaId, resultado.Value(0).RecetaId)
        Assert.AreEqual(a.RecetaId, resultado.Value(1).RecetaId)
    End Function

    <TestMethod>
    Public Async Function BuscarPorPaciente_IdInvalidoOSinRecetas_RetornaListaVacia() As Task
        Dim paciente = SembrarPaciente(Factory)

        Dim invalido = Await _service.BuscarPorPaciente(0)
        Dim sinRecetas = Await _service.BuscarPorPaciente(paciente.PacienteId)

        Assert.IsTrue(invalido.IsSuccess)
        Assert.AreEqual(0, invalido.Value.Count)
        Assert.IsTrue(sinRecetas.IsSuccess)
        Assert.AreEqual(0, sinRecetas.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorConsulta_RetornaSoloLasDeEsaConsulta() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim c1 = SembrarConsulta(Factory, paciente.PacienteId)
        Dim c2 = SembrarConsulta(Factory, paciente.PacienteId)
        Dim deC1 = SembrarRecetaDetallada(Factory, paciente.PacienteId, consultaId:=c1.ConsultaId)
        SembrarRecetaDetallada(Factory, paciente.PacienteId, consultaId:=c2.ConsultaId)

        Dim resultado = Await _service.BuscarPorConsulta(c1.ConsultaId)
        Dim invalido = Await _service.BuscarPorConsulta(0)

        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual(deC1.RecetaId, resultado.Value(0).RecetaId)
        Assert.AreEqual(0, invalido.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCirugia_RetornaSoloLasDeEsaCirugia() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim otra = SembrarCirugiaCompleta(Factory)
        Dim deCirugia = SembrarRecetaDetallada(Factory, cirugia.PacienteId, cirugiaId:=cirugia.CirugiaId)
        SembrarRecetaDetallada(Factory, otra.PacienteId, cirugiaId:=otra.CirugiaId)

        Dim resultado = Await _service.BuscarPorCirugia(cirugia.CirugiaId)
        Dim invalido = Await _service.BuscarPorCirugia(0)

        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual(deCirugia.RecetaId, resultado.Value(0).RecetaId)
        Assert.AreEqual(0, invalido.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorUsuario_RetornaSoloLasQueEmitioEseUsuario() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim laura = SembrarUsuario(Factory, "Laura")
        Dim pedro = SembrarUsuario(Factory, "Pedro")
        Dim deLaura = SembrarRecetaDetallada(Factory, paciente.PacienteId, usuarioId:=laura.UsuarioId)
        SembrarRecetaDetallada(Factory, paciente.PacienteId, usuarioId:=pedro.UsuarioId)
        SembrarRecetaDetallada(Factory, paciente.PacienteId)

        Dim resultado = Await _service.BuscarPorUsuario(laura.UsuarioId)
        Dim invalido = Await _service.BuscarPorUsuario(0)

        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual(deLaura.RecetaId, resultado.Value(0).RecetaId)
        Assert.AreEqual(0, invalido.Value.Count)
    End Function

    ' El rango incluye el día inicial y el día final completos.
    <TestMethod>
    Public Async Function BuscarPorFecha_IncluyeAmbosExtremosYExcluyeLoDemas() As Task
        Dim paciente = SembrarPaciente(Factory)
        SembrarRecetaDetallada(Factory, paciente.PacienteId, fecha:=FechaBase.AddDays(-1))
        Dim inicio = SembrarRecetaDetallada(Factory, paciente.PacienteId, fecha:=FechaBase.Date)
        Dim fin = SembrarRecetaDetallada(Factory, paciente.PacienteId, fecha:=FechaBase.Date.AddDays(2).AddHours(23))
        SembrarRecetaDetallada(Factory, paciente.PacienteId, fecha:=FechaBase.AddDays(3))

        Dim resultado = Await _service.BuscarPorFecha(FechaBase, FechaBase.AddDays(2))

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual(fin.RecetaId, resultado.Value(0).RecetaId)
        Assert.AreEqual(inicio.RecetaId, resultado.Value(1).RecetaId)
    End Function

    ' Rango al revés: no es un error, simplemente no hay resultados.
    <TestMethod>
    Public Async Function BuscarPorFecha_DesdePosteriorAHasta_RetornaListaVacia() As Task
        Dim paciente = SembrarPaciente(Factory)
        SembrarRecetaDetallada(Factory, paciente.PacienteId, fecha:=FechaBase)

        Dim resultado = Await _service.BuscarPorFecha(FechaBase.AddDays(5), FechaBase)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorMedicamento_CoincidenciaParcial_RetornaLasRecetasQueLoContienen() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim conIbuprofeno = SembrarRecetaDetallada(Factory, paciente.PacienteId, medicamento:="Ibuprofeno")
        SembrarRecetaDetallada(Factory, paciente.PacienteId, medicamento:="Amoxicilina")

        Dim resultado = Await _service.BuscarPorMedicamento(" profeno ")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual(conIbuprofeno.RecetaId, resultado.Value(0).RecetaId)
    End Function

    <TestMethod>
    Public Async Function BuscarPorMedicamento_TextoVacioONulo_RetornaListaVacia() As Task
        Dim paciente = SembrarPaciente(Factory)
        SembrarRecetaDetallada(Factory, paciente.PacienteId)

        Dim vacio = Await _service.BuscarPorMedicamento("")
        Dim espacios = Await _service.BuscarPorMedicamento("   ")
        Dim nulo = Await _service.BuscarPorMedicamento(Nothing)

        Assert.AreEqual(0, vacio.Value.Count)
        Assert.AreEqual(0, espacios.Value.Count)
        Assert.AreEqual(0, nulo.Value.Count)
    End Function

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorIndicaciones() As Task
        Dim paciente = SembrarPaciente(Factory)
        SembrarRecetaDetallada(Factory, paciente.PacienteId, indicaciones:="Reposo")
        SembrarRecetaDetallada(Factory, paciente.PacienteId, indicaciones:="Dieta blanda")

        Dim resultado = Await _service.GetList(Function(r) r.Indicaciones = "Dieta blanda")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
    End Function

#End Region

#Region "Eliminar"

    ' Las recetas médicas no se borran: debe avisar con un fallo (no con excepción).
    <TestMethod>
    Public Async Function Eliminar_NoSePermite_RetornaFalloYNoBorraNada() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarRecetaDetallada(Factory, paciente.PacienteId)

        Dim intento = Await Intentar(Function() _service.Eliminar(receta.RecetaId))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Receta)())
        Assert.AreEqual(1, Contar(Of DetallesReceta)())
    End Function

#End Region

End Class
