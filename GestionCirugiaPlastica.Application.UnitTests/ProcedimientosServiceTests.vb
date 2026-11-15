Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class ProcedimientosServiceTests
    Inherits PruebaBase

    Private _service As ProcedimientosServices

    <TestInitialize>
    Public Sub Iniciar()
        _service = New ProcedimientosServices(Factory)
    End Sub

#Region "Guardar - procedimiento nuevo"

    <TestMethod>
    Public Async Function Guardar_ProcedimientoNuevo_LoInsertaRecortandoEspacios() As Task
        Dim resultado = Await _service.Guardar(New Procedimiento With {
            .Nombre = " Rinoplastia ", .Descripcion = " Cirugia de nariz ", .PrecioBase = 50000D})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Procedimiento)()
        Assert.AreEqual(1, guardados.Count)
        Assert.IsTrue(guardados(0).ProcedimientoId > 0, "La base debe generar el Id.")
        Assert.AreEqual("Rinoplastia", guardados(0).Nombre)
        Assert.AreEqual("Cirugia de nariz", guardados(0).Descripcion)
        Assert.AreEqual(50000D, guardados(0).PrecioBase)
        Assert.IsTrue(guardados(0).FechaRegistro.Year >= 2026, "La fecha de registro la pone el servidor.")
    End Function

    <TestMethod>
    Public Async Function Guardar_DescripcionVaciaOEnBlanco_SeGuardaComoNula() As Task
        Dim sinDescripcion = Await _service.Guardar(New Procedimiento With {.Nombre = "Uno", .PrecioBase = 100D})
        Dim enBlanco = Await _service.Guardar(New Procedimiento With {.Nombre = "Dos", .Descripcion = "   ", .PrecioBase = 100D})

        Assert.IsTrue(sinDescripcion.IsSuccess)
        Assert.IsTrue(enBlanco.IsSuccess)
        For Each p In LeerTodos(Of Procedimiento)()
            Assert.IsNull(p.Descripcion)
        Next
    End Function

    <TestMethod>
    Public Async Function Guardar_PrecioCero_SeAcepta() As Task
        Dim resultado = Await _service.Guardar(New Procedimiento With {.Nombre = "Consulta de cortesia", .PrecioBase = 0D})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Procedimiento)())
    End Function

    <TestMethod>
    Public Async Function Guardar_Nulo_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(Nothing))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function Guardar_NombreVacio_RetornaFallo() As Task
        Dim nulo = Await _service.Guardar(New Procedimiento With {.PrecioBase = 100D})
        Dim vacio = Await _service.Guardar(New Procedimiento With {.Nombre = "", .PrecioBase = 100D})
        Dim espacios = Await _service.Guardar(New Procedimiento With {.Nombre = "   ", .PrecioBase = 100D})

        Assert.IsFalse(nulo.IsSuccess)
        Assert.IsFalse(vacio.IsSuccess)
        Assert.IsFalse(espacios.IsSuccess)
        Assert.AreEqual(0, Contar(Of Procedimiento)())
    End Function

    <TestMethod>
    Public Async Function Guardar_NombreDe251Caracteres_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New Procedimiento With {.Nombre = New String("a"c, 251), .PrecioBase = 100D})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Procedimiento)())
    End Function

    <TestMethod>
    Public Async Function Guardar_NombreDe250Caracteres_SeAcepta() As Task
        Dim resultado = Await _service.Guardar(New Procedimiento With {.Nombre = New String("a"c, 250), .PrecioBase = 100D})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Procedimiento)())
    End Function

    <TestMethod>
    Public Async Function Guardar_PrecioNegativo_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New Procedimiento With {.Nombre = "Rinoplastia", .PrecioBase = -1D})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Procedimiento)())
    End Function

    ' El nombre no puede repetirse, sin importar mayúsculas ni espacios sobrantes.
    <TestMethod>
    Public Async Function Guardar_NombreDuplicado_RetornaFalloYNoInserta() As Task
        SembrarProcedimiento(Factory, "Rinoplastia")

        Dim resultado = Await _service.Guardar(New Procedimiento With {.Nombre = "  rinoplastia ", .PrecioBase = 100D})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Procedimiento)())
    End Function

#End Region

#Region "Guardar - procedimiento existente"

    <TestMethod>
    Public Async Function Guardar_ProcedimientoExistente_ActualizaLosDatosSinDuplicar() As Task
        Dim original = SembrarProcedimiento(Factory, "Rinoplastia", 50000D)

        Dim resultado = Await _service.Guardar(New Procedimiento With {
            .ProcedimientoId = original.ProcedimientoId, .Nombre = " Septoplastia ",
            .Descripcion = " Corrige el tabique ", .PrecioBase = 65000D})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Procedimiento)()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual("Septoplastia", guardados(0).Nombre)
        Assert.AreEqual("Corrige el tabique", guardados(0).Descripcion)
        Assert.AreEqual(65000D, guardados(0).PrecioBase)
    End Function

    <TestMethod>
    Public Async Function Guardar_ProcedimientoExistente_ConservaLaFechaDeRegistro() As Task
        Dim original = SembrarProcedimiento(Factory, "Rinoplastia")

        Await _service.Guardar(New Procedimiento With {
            .ProcedimientoId = original.ProcedimientoId, .Nombre = "Rinoplastia", .PrecioBase = 70000D})

        Assert.AreEqual(FechaBase, LeerTodos(Of Procedimiento)()(0).FechaRegistro)
    End Function

    ' Editar solo el precio, dejando el mismo nombre, no cuenta como duplicado.
    <TestMethod>
    Public Async Function Guardar_ProcedimientoExistente_ConSuMismoNombre_NoEsDuplicado() As Task
        Dim original = SembrarProcedimiento(Factory, "Rinoplastia", 50000D)

        Dim resultado = Await _service.Guardar(New Procedimiento With {
            .ProcedimientoId = original.ProcedimientoId, .Nombre = "Rinoplastia", .PrecioBase = 55000D})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(55000D, LeerTodos(Of Procedimiento)()(0).PrecioBase)
    End Function

    <TestMethod>
    Public Async Function Guardar_ProcedimientoExistente_ConElNombreDeOtro_RetornaFallo() As Task
        SembrarProcedimiento(Factory, "Rinoplastia")
        Dim otro = SembrarProcedimiento(Factory, "Mamoplastia")

        Dim resultado = Await _service.Guardar(New Procedimiento With {
            .ProcedimientoId = otro.ProcedimientoId, .Nombre = "RINOPLASTIA", .PrecioBase = 100D})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.IsTrue(LeerTodos(Of Procedimiento)().Any(Function(p) p.Nombre = "Mamoplastia"),
                      "El nombre original no debe cambiar.")
    End Function

    ' Guardar con un ID que no existe no debe intentar insertarlo.
    <TestMethod>
    Public Async Function Guardar_IdInexistente_RetornaFallo() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(New Procedimiento With {
            .ProcedimientoId = 999, .Nombre = "Fantasma", .PrecioBase = 100D}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Procedimiento)())
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_RetornaElProcedimiento() As Task
        Dim procedimiento = SembrarProcedimiento(Factory, "Rinoplastia", 50000D)

        Dim resultado = Await _service.Buscar(procedimiento.ProcedimientoId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Rinoplastia", resultado.Value.Nombre)
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
    Public Async Function GetList_ConCriterio_FiltraYOrdenaPorNombre() As Task
        SembrarProcedimiento(Factory, "Rinoplastia", 50000D)
        SembrarProcedimiento(Factory, "Abdominoplastia", 80000D)
        SembrarProcedimiento(Factory, "Lipoescultura", 30000D)

        Dim resultado = Await _service.GetList(Function(p) p.PrecioBase >= 50000D)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual("Abdominoplastia", resultado.Value(0).Nombre)
        Assert.AreEqual("Rinoplastia", resultado.Value(1).Nombre)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_CoincidenciaParcial_RetornaLosQueContienenElTexto() As Task
        SembrarProcedimiento(Factory, "Rinoplastia")
        SembrarProcedimiento(Factory, "Mamoplastia")
        SembrarProcedimiento(Factory, "Lipoescultura")

        Dim resultado = Await _service.BuscarPorNombre(" plastia ")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual("Mamoplastia", resultado.Value(0).Nombre)
        Assert.AreEqual("Rinoplastia", resultado.Value(1).Nombre)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_TextoVacioONulo_RetornaListaVacia() As Task
        SembrarProcedimiento(Factory, "Rinoplastia")

        Dim vacio = Await _service.BuscarPorNombre("")
        Dim espacios = Await _service.BuscarPorNombre("   ")
        Dim nulo = Await _service.BuscarPorNombre(Nothing)

        Assert.AreEqual(0, vacio.Value.Count)
        Assert.AreEqual(0, espacios.Value.Count)
        Assert.AreEqual(0, nulo.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_SinCoincidencias_RetornaListaVacia() As Task
        SembrarProcedimiento(Factory, "Rinoplastia")

        Dim resultado = Await _service.BuscarPorNombre("xyz")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    ' El rango incluye ambos extremos y ordena del más barato al más caro.
    <TestMethod>
    Public Async Function BuscarPorRangoPrecio_IncluyeExtremosYOrdenaPorPrecio() As Task
        SembrarProcedimiento(Factory, "Barato", 10000D)
        Dim p20 = SembrarProcedimiento(Factory, "Medio", 20000D)
        Dim p30 = SembrarProcedimiento(Factory, "Alto", 30000D)
        SembrarProcedimiento(Factory, "Caro", 40000D)

        Dim resultado = Await _service.BuscarPorRangoPrecio(20000D, 30000D)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual(p20.ProcedimientoId, resultado.Value(0).ProcedimientoId)
        Assert.AreEqual(p30.ProcedimientoId, resultado.Value(1).ProcedimientoId)
    End Function

    ' Rangos inválidos no son error: simplemente no devuelven nada.
    <TestMethod>
    Public Async Function BuscarPorRangoPrecio_RangoInvalido_RetornaListaVacia() As Task
        SembrarProcedimiento(Factory, "Rinoplastia", 50000D)

        Dim alReves = Await _service.BuscarPorRangoPrecio(60000D, 10000D)
        Dim negativo = Await _service.BuscarPorRangoPrecio(-1D, 100000D)

        Assert.IsTrue(alReves.IsSuccess)
        Assert.AreEqual(0, alReves.Value.Count)
        Assert.IsTrue(negativo.IsSuccess)
        Assert.AreEqual(0, negativo.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCirugia_RetornaElProcedimientoDeEsaCirugia() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim rino = SembrarProcedimiento(Factory, "Rinoplastia")
        Dim mamo = SembrarProcedimiento(Factory, "Mamoplastia")
        SembrarCirugia(Factory, paciente.PacienteId, rino.ProcedimientoId)
        Dim cirugiaMamo = SembrarCirugia(Factory, paciente.PacienteId, mamo.ProcedimientoId)

        Dim resultado = Await _service.BuscarPorCirugia(cirugiaMamo.CirugiaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Mamoplastia", resultado.Value.Nombre)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCirugia_CirugiaInexistenteOIdInvalido_RetornaValorNulo() As Task
        SembrarCirugiaCompleta(Factory)

        Dim inexistente = Await _service.BuscarPorCirugia(999)
        Dim cero = Await _service.BuscarPorCirugia(0)

        Assert.IsTrue(inexistente.IsSuccess)
        Assert.IsNull(inexistente.Value)
        Assert.IsTrue(cero.IsSuccess)
        Assert.IsNull(cero.Value)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_SinCirugias_LoQuitaYConservaLosDemas() As Task
        Dim a = SembrarProcedimiento(Factory, "Rinoplastia")
        Dim b = SembrarProcedimiento(Factory, "Mamoplastia")

        Dim resultado = Await _service.Eliminar(a.ProcedimientoId)

        Assert.IsTrue(resultado.IsSuccess)
        Dim quedan = LeerTodos(Of Procedimiento)()
        Assert.AreEqual(1, quedan.Count)
        Assert.AreEqual(b.ProcedimientoId, quedan(0).ProcedimientoId)
    End Function

    ' Con cirugías asociadas no se puede borrar: debe fallar, no lanzar excepción de FK.
    <TestMethod>
    Public Async Function Eliminar_ConCirugiasAsociadas_RetornaFalloYNoBorra() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)

        Dim intento = Await Intentar(Function() _service.Eliminar(cirugia.ProcedimientoId))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Procedimiento)())
    End Function

    <TestMethod>
    Public Async Function Eliminar_IdInexistenteOInvalido_RetornaFalloYNoAfectaOtrosRegistros() As Task
        SembrarProcedimiento(Factory, "Rinoplastia")

        Dim inexistente = Await _service.Eliminar(999)
        Dim cero = Await _service.Eliminar(0)

        Assert.IsFalse(inexistente.IsSuccess)
        Assert.IsFalse(cero.IsSuccess)
        Assert.AreEqual(1, Contar(Of Procedimiento)())
    End Function

#End Region

End Class
