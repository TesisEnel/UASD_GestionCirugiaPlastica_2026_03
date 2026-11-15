Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class FotografiasServiceTests
    Inherits PruebaBase

    Private _service As FotografiasService

    <TestInitialize>
    Public Sub Iniciar()
        _service = New FotografiasService(Factory)
    End Sub

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_FotografiaNueva_LaInsertaRecortandoEspacios() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim foto As New Fotografia With {
            .CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase, .Tipo = " Despues ",
            .RutaArchivo = "  fotos/despues.jpg ", .Descripcion = " Vista frontal "}

        Dim resultado = Await _service.Guardar(foto)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardadas = LeerTodos(Of Fotografia)()
        Assert.AreEqual(1, guardadas.Count)
        Assert.IsTrue(guardadas(0).FotografiaId > 0, "La base debe generar el FotografiaId.")
        Assert.AreEqual("Despues", guardadas(0).Tipo)
        Assert.AreEqual("fotos/despues.jpg", guardadas(0).RutaArchivo)
        Assert.AreEqual("Vista frontal", guardadas(0).Descripcion)
        Assert.AreEqual(FechaBase, guardadas(0).Fecha)
    End Function

    ' Sin fecha se usa la actual (como el default getdate() de la base), no 01/01/0001.
    <TestMethod>
    Public Async Function Guardar_NuevaSinFecha_AsignaLaFechaActual() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim foto As New Fotografia With {.CirugiaId = cirugia.CirugiaId, .RutaArchivo = "fotos/a.jpg"}

        Dim resultado = Await _service.Guardar(foto)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardada = LeerTodos(Of Fotografia)()(0)
        Assert.IsTrue(guardada.Fecha.Year >= 2026, "No debe quedar 0001-01-01.")
    End Function

    <TestMethod>
    Public Async Function Guardar_FotografiaExistente_ActualizaLaDescripcionSinDuplicar() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim original = SembrarFotografia(Factory, cirugia.CirugiaId)
        Dim modificada As New Fotografia With {
            .FotografiaId = original.FotografiaId, .CirugiaId = cirugia.CirugiaId,
            .Fecha = original.Fecha, .Tipo = original.Tipo, .RutaArchivo = original.RutaArchivo,
            .Descripcion = "Vista lateral"}

        Dim resultado = Await _service.Guardar(modificada)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardadas = LeerTodos(Of Fotografia)()
        Assert.AreEqual(1, guardadas.Count)
        Assert.AreEqual("Vista lateral", guardadas(0).Descripcion)
    End Function

    ' Al editar sin mandar fecha no se debe pisar la original con 01/01/0001.
    <TestMethod>
    Public Async Function Guardar_ExistenteSinFecha_ConservaLaFechaOriginal() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim original = SembrarFotografia(Factory, cirugia.CirugiaId)
        Dim modificada As New Fotografia With {
            .FotografiaId = original.FotografiaId, .CirugiaId = cirugia.CirugiaId,
            .RutaArchivo = original.RutaArchivo, .Descripcion = "Nueva"}

        Dim resultado = Await _service.Guardar(modificada)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(original.Fecha, LeerTodos(Of Fotografia)()(0).Fecha)
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_Fotografias_Cirugias).
    <TestMethod>
    Public Async Function Guardar_CirugiaInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim foto As New Fotografia With {.CirugiaId = 999, .RutaArchivo = "fotos/a.jpg", .Fecha = FechaBase}

        Dim intento = Await Intentar(Function() _service.Guardar(foto))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Fotografia)())
    End Function

    <TestMethod>
    Public Async Function Guardar_SinCirugia_RetornaFallo() As Task
        Dim foto As New Fotografia With {.RutaArchivo = "fotos/a.jpg", .Fecha = FechaBase}

        Dim resultado = Await _service.Guardar(foto)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Fotografia)())
    End Function

    ' REGLA PENDIENTE: RutaArchivo es NOT NULL en la base.
    <TestMethod>
    Public Async Function Guardar_RutaVaciaONula_RetornaFalloSinLanzarExcepcion() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim vacia As New Fotografia With {.CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase, .RutaArchivo = "   "}
        Dim nula As New Fotografia With {.CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase}

        Dim r1 = Await Intentar(Function() _service.Guardar(vacia))
        Dim r2 = Await Intentar(Function() _service.Guardar(nula))

        Assert.IsNull(r1.Excepcion, $"No debería lanzar: {r1.Excepcion?.GetType().Name}")
        Assert.IsNull(r2.Excepcion, $"No debería lanzar: {r2.Excepcion?.GetType().Name}")
        Assert.IsFalse(r1.Resultado.IsSuccess)
        Assert.IsFalse(r2.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Fotografia)())
    End Function

    ' REGLA PENDIENTE: ruta, tipo y descripción tienen máximo de 250 caracteres.
    <TestMethod>
    Public Async Function Guardar_TextosDeMasDe250Caracteres_RetornaFallo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim largo = New String("x"c, 251)
        Dim conRuta As New Fotografia With {.CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase, .RutaArchivo = largo}
        Dim conTipo As New Fotografia With {.CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase, .RutaArchivo = "a.jpg", .Tipo = largo}
        Dim conDescripcion As New Fotografia With {.CirugiaId = cirugia.CirugiaId, .Fecha = FechaBase, .RutaArchivo = "a.jpg", .Descripcion = largo}

        Assert.IsFalse((Await _service.Guardar(conRuta)).IsSuccess, "RutaArchivo")
        Assert.IsFalse((Await _service.Guardar(conTipo)).IsSuccess, "Tipo")
        Assert.IsFalse((Await _service.Guardar(conDescripcion)).IsSuccess, "Descripcion")
        Assert.AreEqual(0, Contar(Of Fotografia)())
    End Function

    ' Guardar con un ID que no existe no debe intentar insertarlo.
    <TestMethod>
    Public Async Function Guardar_FotografiaIdInexistente_RetornaFallo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim foto As New Fotografia With {
            .FotografiaId = 999, .CirugiaId = cirugia.CirugiaId, .RutaArchivo = "a.jpg", .Fecha = FechaBase}

        Dim intento = Await Intentar(Function() _service.Guardar(foto))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Fotografia)())
    End Function

    ' Si el JSON trae la Cirugía anidada (como la devuelve Buscar), no debe modificarla.
    <TestMethod>
    Public Async Function Guardar_ConCirugiaAnidada_NoDebeModificarLaCirugia() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim original = SembrarFotografia(Factory, cirugia.CirugiaId)
        Dim modificada As New Fotografia With {
            .FotografiaId = original.FotografiaId, .CirugiaId = cirugia.CirugiaId,
            .Fecha = original.Fecha, .RutaArchivo = original.RutaArchivo, .Descripcion = "Nueva",
            .Cirugia = New Cirugia With {
                .CirugiaId = cirugia.CirugiaId, .PacienteId = cirugia.PacienteId,
                .ProcedimientoId = cirugia.ProcedimientoId, .FechaCirugia = cirugia.FechaCirugia,
                .Estado = "Alterado"}}

        Await _service.Guardar(modificada)

        Using contexto = Factory.CreateDbContext()
            Dim enBase = contexto.Cirugias.Find(cirugia.CirugiaId)
            Assert.AreEqual("Pendiente", enBase.Estado, "Guardar una foto no debe cambiar la cirugía.")
        End Using
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyeCirugiaPacienteYProcedimiento() As Task
        Dim paciente = SembrarPaciente(Factory, nombre:="Ana")
        Dim procedimiento = SembrarProcedimiento(Factory, nombre:="Rinoplastia")
        Dim cirugia = SembrarCirugia(Factory, paciente.PacienteId, procedimiento.ProcedimientoId)
        Dim foto = SembrarFotografia(Factory, cirugia.CirugiaId)

        Dim resultado = Await _service.Buscar(foto.FotografiaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Ana", resultado.Value.Cirugia.Paciente.Nombre)
        Assert.AreEqual("Rinoplastia", resultado.Value.Cirugia.Procedimiento.Nombre)
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
        Dim foto = SembrarFotografia(Factory, cirugia.CirugiaId)
        Dim resultado = Await _service.Buscar(foto.FotografiaId)
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

    ' Nota: Contains en SQLite distingue mayúsculas; en SQL Server no. Por eso se usa la misma capitalización.
    <TestMethod>
    Public Async Function BuscarPorNombre_CoincideConNombreOApellidoDelPaciente() As Task
        Dim ana = SembrarPaciente(Factory, nombre:="Ana", apellido:="Lopez")
        Dim luis = SembrarPaciente(Factory, nombre:="Luis", apellido:="Mota")
        Dim procedimiento = SembrarProcedimiento(Factory)
        Dim cirugiaAna = SembrarCirugia(Factory, ana.PacienteId, procedimiento.ProcedimientoId)
        Dim cirugiaLuis = SembrarCirugia(Factory, luis.PacienteId, procedimiento.ProcedimientoId)
        SembrarFotografia(Factory, cirugiaAna.CirugiaId, "fotos/ana.jpg")
        SembrarFotografia(Factory, cirugiaLuis.CirugiaId, "fotos/luis.jpg")

        Dim porNombre = Await _service.BuscarPorNombre("Ana")
        Dim porApellido = Await _service.BuscarPorNombre("Mota")

        Assert.AreEqual(1, porNombre.Value.Count)
        Assert.AreEqual("fotos/ana.jpg", porNombre.Value(0).RutaArchivo)
        Assert.AreEqual(1, porApellido.Value.Count)
        Assert.AreEqual("fotos/luis.jpg", porApellido.Value(0).RutaArchivo)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_SinCoincidencias_RetornaListaVacia() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        SembrarFotografia(Factory, cirugia.CirugiaId)

        Dim resultado = Await _service.BuscarPorNombre("Zzz")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCedula_RetornaSoloLasFotosDeEsaCedula() As Task
        Dim cirugiaAna = SembrarCirugiaCompleta(Factory, cedula:="001-1111111-1")
        Dim cirugiaLuis = SembrarCirugiaCompleta(Factory, cedula:="001-2222222-2")
        SembrarFotografia(Factory, cirugiaAna.CirugiaId, "fotos/a1.jpg")
        SembrarFotografia(Factory, cirugiaAna.CirugiaId, "fotos/a2.jpg")
        SembrarFotografia(Factory, cirugiaLuis.CirugiaId, "fotos/l1.jpg")

        Dim resultado = Await _service.BuscarPorCedula("001-1111111-1")

        Assert.AreEqual(2, resultado.Value.Count)
        Assert.IsTrue(resultado.Value.All(Function(f) f.CirugiaId = cirugiaAna.CirugiaId))
    End Function

#End Region

#Region "GetList"

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorTipo() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        SembrarFotografia(Factory, cirugia.CirugiaId, "fotos/a.jpg", "Antes")
        SembrarFotografia(Factory, cirugia.CirugiaId, "fotos/b.jpg", "Despues")

        Dim resultado = Await _service.GetList(Function(f) f.Tipo = "Despues")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual("fotos/b.jpg", resultado.Value(0).RutaArchivo)
        Assert.IsNotNull(resultado.Value(0).Cirugia.Paciente)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_FotografiaExistente_LaQuitaYConservaLasDemas() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        Dim a = SembrarFotografia(Factory, cirugia.CirugiaId, "fotos/a.jpg")
        Dim b = SembrarFotografia(Factory, cirugia.CirugiaId, "fotos/b.jpg")

        Dim resultado = Await _service.Eliminar(a.FotografiaId)

        Assert.IsTrue(resultado.IsSuccess)
        Dim quedan = LeerTodos(Of Fotografia)()
        Assert.AreEqual(1, quedan.Count)
        Assert.AreEqual(b.FotografiaId, quedan(0).FotografiaId)
    End Function

    ' Borrar algo que no existe debe reportar fallo, no éxito.
    <TestMethod>
    Public Async Function Eliminar_IdInexistente_RetornaFalloYNoAfectaOtrosRegistros() As Task
        Dim cirugia = SembrarCirugiaCompleta(Factory)
        SembrarFotografia(Factory, cirugia.CirugiaId)

        Dim resultado = Await _service.Eliminar(999)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Fotografia)())
    End Function

#End Region

End Class
