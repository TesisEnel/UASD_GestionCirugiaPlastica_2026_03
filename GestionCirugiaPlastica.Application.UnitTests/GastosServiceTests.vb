Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class GastosServiceTests
    Inherits PruebaBase

    Private _service As GastosService

    <TestInitialize>
    Public Sub Iniciar()
        _service = New GastosService(Factory)
    End Sub

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_GastoNuevo_LoInsertaRecortandoEspacios() As Task
        Dim gasto As New Gasto With {
            .Fecha = FechaBase, .Concepto = "  Guantes ", .Categoria = " Insumos ",
            .Monto = 350.5D, .MetodoPago = " Efectivo "}

        Dim resultado = Await _service.Guardar(gasto)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Gasto)()
        Assert.AreEqual(1, guardados.Count)
        Assert.IsTrue(guardados(0).GastoId > 0, "La base debe generar el GastoId.")
        Assert.AreEqual("Guantes", guardados(0).Concepto)
        Assert.AreEqual("Insumos", guardados(0).Categoria)
        Assert.AreEqual("Efectivo", guardados(0).MetodoPago)
        Assert.AreEqual(350.5D, guardados(0).Monto)
        Assert.AreEqual(FechaBase, guardados(0).Fecha)
    End Function

    ' Sin fecha se usa la actual (como el default getdate() de la base), no 01/01/0001.
    <TestMethod>
    Public Async Function Guardar_NuevoSinFecha_AsignaLaFechaActual() As Task
        Dim gasto As New Gasto With {.Concepto = "Luz", .Monto = 100D}

        Dim resultado = Await _service.Guardar(gasto)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsTrue(LeerTodos(Of Gasto)()(0).Fecha.Year >= 2026, "No debe quedar 0001-01-01.")
    End Function

    <TestMethod>
    Public Async Function Guardar_GastoExistente_ActualizaElMontoSinDuplicar() As Task
        Dim original = SembrarGasto(Factory)
        Dim modificado As New Gasto With {
            .GastoId = original.GastoId, .Fecha = original.Fecha,
            .Concepto = original.Concepto, .Monto = 999D}

        Dim resultado = Await _service.Guardar(modificado)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Gasto)()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual(999D, guardados(0).Monto)
    End Function

    ' Al editar sin mandar fecha no se debe pisar la original con 01/01/0001.
    <TestMethod>
    Public Async Function Guardar_ExistenteSinFecha_ConservaLaFechaOriginal() As Task
        Dim original = SembrarGasto(Factory)
        Dim modificado As New Gasto With {
            .GastoId = original.GastoId, .Concepto = "Otro concepto", .Monto = 50D}

        Dim resultado = Await _service.Guardar(modificado)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(original.Fecha, LeerTodos(Of Gasto)()(0).Fecha)
    End Function

    ' REGLA PENDIENTE: Concepto es NOT NULL en la base.
    <TestMethod>
    Public Async Function Guardar_ConceptoVacioONulo_RetornaFalloSinLanzarExcepcion() As Task
        Dim vacio As New Gasto With {.Fecha = FechaBase, .Concepto = "   ", .Monto = 10D}
        Dim nulo As New Gasto With {.Fecha = FechaBase, .Monto = 10D}

        Dim r1 = Await Intentar(Function() _service.Guardar(vacio))
        Dim r2 = Await Intentar(Function() _service.Guardar(nulo))

        Assert.IsNull(r1.Excepcion, $"No debería lanzar: {r1.Excepcion?.GetType().Name}")
        Assert.IsNull(r2.Excepcion, $"No debería lanzar: {r2.Excepcion?.GetType().Name}")
        Assert.IsFalse(r1.Resultado.IsSuccess)
        Assert.IsFalse(r2.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Gasto)())
    End Function

    ' REGLA PENDIENTE: un gasto de monto cero o negativo no tiene sentido.
    <TestMethod>
    Public Async Function Guardar_MontoCeroONegativo_RetornaFallo() As Task
        Dim cero As New Gasto With {.Fecha = FechaBase, .Concepto = "A", .Monto = 0D}
        Dim negativo As New Gasto With {.Fecha = FechaBase, .Concepto = "B", .Monto = -50D}

        Assert.IsFalse((Await _service.Guardar(cero)).IsSuccess, "Cero")
        Assert.IsFalse((Await _service.Guardar(negativo)).IsSuccess, "Negativo")
        Assert.AreEqual(0, Contar(Of Gasto)())
    End Function

    ' REGLA PENDIENTE: concepto, categoría y método de pago tienen máximo de 250 caracteres.
    <TestMethod>
    Public Async Function Guardar_TextosDeMasDe250Caracteres_RetornaFallo() As Task
        Dim largo = New String("x"c, 251)
        Dim conConcepto As New Gasto With {.Fecha = FechaBase, .Monto = 1D, .Concepto = largo}
        Dim conCategoria As New Gasto With {.Fecha = FechaBase, .Monto = 1D, .Concepto = "A", .Categoria = largo}
        Dim conMetodo As New Gasto With {.Fecha = FechaBase, .Monto = 1D, .Concepto = "A", .MetodoPago = largo}

        Assert.IsFalse((Await _service.Guardar(conConcepto)).IsSuccess, "Concepto")
        Assert.IsFalse((Await _service.Guardar(conCategoria)).IsSuccess, "Categoria")
        Assert.IsFalse((Await _service.Guardar(conMetodo)).IsSuccess, "MetodoPago")
        Assert.AreEqual(0, Contar(Of Gasto)())
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_Gastos_Usuarios).
    <TestMethod>
    Public Async Function Guardar_UsuarioInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim gasto As New Gasto With {.Fecha = FechaBase, .Concepto = "A", .Monto = 10D, .UsuarioId = 999}

        Dim intento = Await Intentar(Function() _service.Guardar(gasto))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Gasto)())
    End Function

    <TestMethod>
    Public Async Function Guardar_ConUsuarioValido_LoGuardaConElUsuario() As Task
        Dim usuario = SembrarUsuario(Factory)
        Dim gasto As New Gasto With {.Fecha = FechaBase, .Concepto = "A", .Monto = 10D, .UsuarioId = usuario.UsuarioId}

        Dim resultado = Await _service.Guardar(gasto)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(usuario.UsuarioId, LeerTodos(Of Gasto)()(0).UsuarioId)
    End Function

    ' Guardar con un ID que no existe no debe intentar insertarlo.
    <TestMethod>
    Public Async Function Guardar_GastoIdInexistente_RetornaFallo() As Task
        Dim gasto As New Gasto With {.GastoId = 999, .Fecha = FechaBase, .Concepto = "A", .Monto = 10D}

        Dim intento = Await Intentar(Function() _service.Guardar(gasto))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Gasto)())
    End Function

    ' Si el JSON trae el Usuario anidado (como lo devuelve Buscar), no debe modificarlo.
    <TestMethod>
    Public Async Function Guardar_ConUsuarioAnidado_NoDebeModificarElUsuario() As Task
        Dim usuario = SembrarUsuario(Factory, nombre:="Laura")
        Dim original = SembrarGasto(Factory, usuarioId:=usuario.UsuarioId)
        Dim modificado As New Gasto With {
            .GastoId = original.GastoId, .Fecha = original.Fecha, .Concepto = original.Concepto,
            .Monto = 77D, .UsuarioId = usuario.UsuarioId,
            .Usuario = New Usuario With {
                .UsuarioId = usuario.UsuarioId, .Nombre = "Alterado", .Apellido = usuario.Apellido,
                .Email = usuario.Email, .PasswordHash = "x", .RolId = usuario.RolId}}

        Await _service.Guardar(modificado)

        Using contexto = Factory.CreateDbContext()
            Assert.AreEqual("Laura", contexto.Usuarios.Find(usuario.UsuarioId).Nombre,
                            "Guardar un gasto no debe cambiar al usuario.")
        End Using
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyeAlUsuario() As Task
        Dim usuario = SembrarUsuario(Factory, nombre:="Laura")
        Dim gasto = SembrarGasto(Factory, usuarioId:=usuario.UsuarioId)

        Dim resultado = Await _service.Buscar(gasto.GastoId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Laura", resultado.Value.Usuario.Nombre)
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
        Dim usuario = SembrarUsuario(Factory)
        Dim gasto = SembrarGasto(Factory, usuarioId:=usuario.UsuarioId)
        Dim resultado = Await _service.Buscar(gasto.GastoId)
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

#Region "Busquedas por fecha"

    <TestMethod>
    Public Async Function BuscarPorFecha_RetornaSoloLosGastosDeEseDia() As Task
        SembrarGasto(Factory, "Hoy 1", FechaBase)
        SembrarGasto(Factory, "Hoy 2", FechaBase.AddHours(8))
        SembrarGasto(Factory, "Manana", FechaBase.AddDays(1))
        SembrarGasto(Factory, "Ayer", FechaBase.AddDays(-1))

        Dim resultado = Await _service.BuscarPorFecha(FechaBase)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorRangoFechas_IncluyeElDiaFinalCompleto() As Task
        SembrarGasto(Factory, "Dentro 1", FechaBase)
        SembrarGasto(Factory, "Dentro 2", FechaBase.AddDays(1).AddHours(8))
        SembrarGasto(Factory, "Fuera despues", FechaBase.AddDays(2))
        SembrarGasto(Factory, "Fuera antes", FechaBase.AddDays(-1))

        Dim resultado = Await _service.BuscarPorRangoFechas(FechaBase, FechaBase.AddDays(1))

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorRangoFechas_RangoInvertido_RetornaListaVacia() As Task
        SembrarGasto(Factory, "A", FechaBase)

        Dim resultado = Await _service.BuscarPorRangoFechas(FechaBase.AddDays(5), FechaBase)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

#End Region

#Region "GetList"

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorConcepto() As Task
        SembrarGasto(Factory, "Insumos", FechaBase, 100D)
        SembrarGasto(Factory, "Alquiler", FechaBase, 5000D)

        Dim resultado = Await _service.GetList(Function(g) g.Concepto = "Alquiler")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual(5000D, resultado.Value(0).Monto)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_GastoExistente_LoQuitaYConservaLosDemas() As Task
        Dim a = SembrarGasto(Factory, "A")
        Dim b = SembrarGasto(Factory, "B")

        Dim resultado = Await _service.Eliminar(a.GastoId)

        Assert.IsTrue(resultado.IsSuccess)
        Dim quedan = LeerTodos(Of Gasto)()
        Assert.AreEqual(1, quedan.Count)
        Assert.AreEqual(b.GastoId, quedan(0).GastoId)
    End Function

    ' Borrar algo que no existe debe reportar fallo, no éxito.
    <TestMethod>
    Public Async Function Eliminar_IdInexistente_RetornaFalloYNoAfectaOtrosRegistros() As Task
        SembrarGasto(Factory)

        Dim resultado = Await _service.Eliminar(999)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Gasto)())
    End Function

#End Region

End Class
