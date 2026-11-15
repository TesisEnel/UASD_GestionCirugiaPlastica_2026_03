Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class DetallesRecetaServiceTests
    Inherits PruebaBase

    Private _service As DetallesRecetaService

    <TestInitialize>
    Public Sub Iniciar()
        _service = New DetallesRecetaService(Factory)
    End Sub

    ' SembrarReceta crea la receta con UN detalle ("Ibuprofeno"); aquí se devuelve ese detalle.
    Private Function PrimerDetalle(receta As Receta) As DetallesReceta
        Return receta.DetallesReceta.First()
    End Function

    ' Agrega un detalle más a una receta ya creada.
    Private Function AgregarDetalle(recetaId As Integer, medicamento As String) As DetallesReceta
        Dim detalle As New DetallesReceta With {.RecetaId = recetaId, .Medicamento = medicamento}
        Using contexto = Factory.CreateDbContext()
            contexto.DetallesReceta.Add(detalle)
            contexto.SaveChanges()
        End Using
        Return detalle
    End Function

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_DetalleNuevo_LoInserta() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarReceta(Factory, paciente.PacienteId)
        Dim detalle As New DetallesReceta With {
            .RecetaId = receta.RecetaId, .Medicamento = "Amoxicilina", .Dosis = "500 mg",
            .Frecuencia = "Cada 8 horas", .Duracion = "7 dias"}

        Dim resultado = Await _service.Guardar(detalle)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, Contar(Of DetallesReceta)())
        Assert.IsTrue(LeerTodos(Of DetallesReceta)().Any(Function(d) d.Medicamento = "Amoxicilina"))
    End Function

    <TestMethod>
    Public Async Function Guardar_DetalleExistente_ActualizaLaDosisSinDuplicar() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarReceta(Factory, paciente.PacienteId)
        Dim original = PrimerDetalle(receta)
        Dim modificado As New DetallesReceta With {
            .DetalleRecetaId = original.DetalleRecetaId, .RecetaId = receta.RecetaId,
            .Medicamento = original.Medicamento, .Dosis = "800 mg"}

        Dim resultado = Await _service.Guardar(modificado)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of DetallesReceta)()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual("800 mg", guardados(0).Dosis)
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_DetallesReceta_Recetas).
    <TestMethod>
    Public Async Function Guardar_RecetaInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim detalle As New DetallesReceta With {.RecetaId = 999, .Medicamento = "Paracetamol"}

        Dim intento = Await Intentar(Function() _service.Guardar(detalle))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of DetallesReceta)())
    End Function

    ' REGLA PENDIENTE: el medicamento es obligatorio (columna NOT NULL en la base).
    <TestMethod>
    Public Async Function Guardar_MedicamentoVacio_RetornaFalloSinLanzarExcepcion() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarReceta(Factory, paciente.PacienteId)
        Dim vacio As New DetallesReceta With {.RecetaId = receta.RecetaId, .Medicamento = "   "}
        Dim nulo As New DetallesReceta With {.RecetaId = receta.RecetaId}

        Dim r1 = Await Intentar(Function() _service.Guardar(vacio))
        Dim r2 = Await Intentar(Function() _service.Guardar(nulo))

        Assert.IsNull(r1.Excepcion, $"No debería lanzar: {r1.Excepcion?.GetType().Name}")
        Assert.IsNull(r2.Excepcion, $"No debería lanzar: {r2.Excepcion?.GetType().Name}")
        Assert.IsFalse(r1.Resultado.IsSuccess)
        Assert.IsFalse(r2.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of DetallesReceta)(), "Solo debe quedar el detalle original.")
    End Function

    ' REGLA PENDIENTE: todos los textos tienen máximo de 250 caracteres en la base.
    <TestMethod>
    Public Async Function Guardar_TextosDeMasDe250Caracteres_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarReceta(Factory, paciente.PacienteId)
        Dim largo = New String("x"c, 251)
        Dim conMedicamento As New DetallesReceta With {.RecetaId = receta.RecetaId, .Medicamento = largo}
        Dim conDosis As New DetallesReceta With {.RecetaId = receta.RecetaId, .Medicamento = "A", .Dosis = largo}
        Dim conFrecuencia As New DetallesReceta With {.RecetaId = receta.RecetaId, .Medicamento = "A", .Frecuencia = largo}
        Dim conDuracion As New DetallesReceta With {.RecetaId = receta.RecetaId, .Medicamento = "A", .Duracion = largo}
        Dim conInstrucciones As New DetallesReceta With {.RecetaId = receta.RecetaId, .Medicamento = "A", .Instrucciones = largo}

        Assert.IsFalse((Await _service.Guardar(conMedicamento)).IsSuccess, "Medicamento")
        Assert.IsFalse((Await _service.Guardar(conDosis)).IsSuccess, "Dosis")
        Assert.IsFalse((Await _service.Guardar(conFrecuencia)).IsSuccess, "Frecuencia")
        Assert.IsFalse((Await _service.Guardar(conDuracion)).IsSuccess, "Duracion")
        Assert.IsFalse((Await _service.Guardar(conInstrucciones)).IsSuccess, "Instrucciones")
        Assert.AreEqual(1, Contar(Of DetallesReceta)())
    End Function

    ' REGLA PENDIENTE: guardar un detalle con un ID que no existe no debe intentar insertarlo.
    <TestMethod>
    Public Async Function Guardar_DetalleIdInexistente_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarReceta(Factory, paciente.PacienteId)
        Dim detalle As New DetallesReceta With {
            .DetalleRecetaId = 999, .RecetaId = receta.RecetaId, .Medicamento = "Paracetamol"}

        Dim intento = Await Intentar(Function() _service.Guardar(detalle))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    ' REGLA PROPUESTA: un detalle no se puede mover de una receta a otra
    ' (RecetasServices ya lo impide al editar desde la receta).
    <TestMethod>
    Public Async Function Guardar_CambiarDeReceta_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim recetaA = SembrarReceta(Factory, paciente.PacienteId, "Ibuprofeno")
        Dim recetaB = SembrarReceta(Factory, paciente.PacienteId, "Naproxeno")
        Dim original = PrimerDetalle(recetaA)
        Dim movido As New DetallesReceta With {
            .DetalleRecetaId = original.DetalleRecetaId, .RecetaId = recetaB.RecetaId, .Medicamento = "Ibuprofeno"}

        Dim resultado = Await _service.Guardar(movido)

        Assert.IsFalse(resultado.IsSuccess)
        Dim enBase = LeerTodos(Of DetallesReceta)().First(Function(d) d.DetalleRecetaId = original.DetalleRecetaId)
        Assert.AreEqual(recetaA.RecetaId, enBase.RecetaId)
    End Function

    ' BUG PROBABLE: Guardar usa DbSet.Update(detalle). Si el JSON trae la Receta
    ' anidada (como la devuelve Buscar), EF también la marca como modificada.
    <TestMethod>
    Public Async Function Guardar_ConRecetaAnidada_NoDebeModificarLaReceta() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarReceta(Factory, paciente.PacienteId)
        Dim original = PrimerDetalle(receta)
        Dim modificado As New DetallesReceta With {
            .DetalleRecetaId = original.DetalleRecetaId, .RecetaId = receta.RecetaId,
            .Medicamento = original.Medicamento, .Dosis = "800 mg",
            .Receta = New Receta With {
                .RecetaId = receta.RecetaId, .PacienteId = paciente.PacienteId,
                .Fecha = receta.Fecha, .Indicaciones = "Alterado"}}

        Await _service.Guardar(modificado)

        Using contexto = Factory.CreateDbContext()
            Dim enBase = contexto.Recetas.Find(receta.RecetaId)
            Assert.IsNull(enBase.Indicaciones, "Guardar un detalle no debe cambiar los datos de la receta.")
        End Using
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyeRecetaYPaciente() As Task
        Dim paciente = SembrarPaciente(Factory, nombre:="Ana")
        Dim receta = SembrarReceta(Factory, paciente.PacienteId)

        Dim resultado = Await _service.Buscar(PrimerDetalle(receta).DetalleRecetaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Ana", resultado.Value.Receta.Paciente.Nombre)
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
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarReceta(Factory, paciente.PacienteId)
        Dim resultado = Await _service.Buscar(PrimerDetalle(receta).DetalleRecetaId)
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
    Public Async Function BuscarPorNombre_CoincideConElPacienteDeLaReceta() As Task
        Dim ana = SembrarPaciente(Factory, nombre:="Ana", apellido:="Lopez")
        Dim luis = SembrarPaciente(Factory, nombre:="Luis", apellido:="Mota")
        SembrarReceta(Factory, ana.PacienteId, "Ibuprofeno")
        SembrarReceta(Factory, luis.PacienteId, "Naproxeno")

        Dim porNombre = Await _service.BuscarPorNombre("Ana")
        Dim porApellido = Await _service.BuscarPorNombre("Mota")

        Assert.AreEqual(1, porNombre.Value.Count)
        Assert.AreEqual("Ibuprofeno", porNombre.Value(0).Medicamento)
        Assert.AreEqual(1, porApellido.Value.Count)
        Assert.AreEqual("Naproxeno", porApellido.Value(0).Medicamento)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_SinCoincidencias_RetornaListaVacia() As Task
        Dim ana = SembrarPaciente(Factory, nombre:="Ana")
        SembrarReceta(Factory, ana.PacienteId)

        Dim resultado = Await _service.BuscarPorNombre("Zzz")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCedula_RetornaSoloLosDetallesDeEsaCedula() As Task
        Dim ana = SembrarPaciente(Factory, cedula:="001-1111111-1")
        Dim luis = SembrarPaciente(Factory, cedula:="001-2222222-2")
        Dim recetaAna = SembrarReceta(Factory, ana.PacienteId, "Ibuprofeno")
        AgregarDetalle(recetaAna.RecetaId, "Amoxicilina")
        SembrarReceta(Factory, luis.PacienteId, "Naproxeno")

        Dim resultado = Await _service.BuscarPorCedula("001-1111111-1")

        Assert.AreEqual(2, resultado.Value.Count)
        Assert.IsTrue(resultado.Value.All(Function(d) d.RecetaId = recetaAna.RecetaId))
    End Function

#End Region

#Region "GetList"

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorMedicamento() As Task
        Dim paciente = SembrarPaciente(Factory)
        SembrarReceta(Factory, paciente.PacienteId, "Ibuprofeno")
        SembrarReceta(Factory, paciente.PacienteId, "Naproxeno")

        Dim resultado = Await _service.GetList(Function(d) d.Medicamento.StartsWith("Nap"))

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.IsNotNull(resultado.Value(0).Receta.Paciente)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_DetalleExistenteConOtrosEnLaReceta_LoQuita() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarReceta(Factory, paciente.PacienteId, "Ibuprofeno")
        Dim extra = AgregarDetalle(receta.RecetaId, "Amoxicilina")

        Dim resultado = Await _service.Eliminar(extra.DetalleRecetaId)

        Assert.IsTrue(resultado.IsSuccess)
        Dim quedan = LeerTodos(Of DetallesReceta)()
        Assert.AreEqual(1, quedan.Count)
        Assert.AreEqual("Ibuprofeno", quedan(0).Medicamento)
    End Function

    <TestMethod>
    Public Async Function Eliminar_IdInexistente_NoAfectaOtrosRegistros() As Task
        Dim paciente = SembrarPaciente(Factory)
        SembrarReceta(Factory, paciente.PacienteId)

        Await _service.Eliminar(999)

        Assert.AreEqual(1, Contar(Of DetallesReceta)())
    End Function

    ' REGLA PENDIENTE: borrar algo que no existe debe reportar fallo, no éxito.
    <TestMethod>
    Public Async Function Eliminar_IdInexistente_RetornaFallo() As Task
        Dim resultado = Await _service.Eliminar(999)

        Assert.IsFalse(resultado.IsSuccess)
    End Function

    ' REGLA PROPUESTA: una receta debe conservar al menos un medicamento
    ' (RecetasServices.Guardar ya exige al menos un detalle).
    <TestMethod>
    Public Async Function Eliminar_UltimoDetalleDeLaReceta_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim receta = SembrarReceta(Factory, paciente.PacienteId)

        Dim resultado = Await _service.Eliminar(PrimerDetalle(receta).DetalleRecetaId)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of DetallesReceta)())
    End Function

#End Region

End Class
