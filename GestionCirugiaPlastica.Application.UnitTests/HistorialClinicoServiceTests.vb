Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class HistorialClinicoServiceTests
    Inherits PruebaBase

    Private _service As HistorialClinicoService

    <TestInitialize>
    Public Sub Iniciar()
        _service = New HistorialClinicoService(Factory)
    End Sub

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_HistorialNuevo_LoInsertaRecortandoEspacios() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim historial As New HistorialClinico With {
            .PacienteId = paciente.PacienteId, .Alergias = " Penicilina ",
            .Antecedentes = " Hipertension ", .Medicamentos = " Enalapril ", .NotasMedicas = " Control anual "}

        Dim resultado = Await _service.Guardar(historial)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of HistorialClinico)()
        Assert.AreEqual(1, guardados.Count)
        Assert.IsTrue(guardados(0).HistoriaId > 0, "La base debe generar el HistoriaId.")
        Assert.AreEqual("Penicilina", guardados(0).Alergias)
        Assert.AreEqual("Hipertension", guardados(0).Antecedentes)
        Assert.AreEqual("Enalapril", guardados(0).Medicamentos)
        Assert.AreEqual("Control anual", guardados(0).NotasMedicas)
        Assert.IsTrue(guardados(0).FechaActualizacion.Year >= 2026, "La fecha la pone el servidor.")
    End Function

    <TestMethod>
    Public Async Function Guardar_HistorialExistente_ActualizaLosDatosSinDuplicar() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim original = SembrarHistorial(Factory, paciente.PacienteId)
        Dim modificado As New HistorialClinico With {
            .HistoriaId = original.HistoriaId, .PacienteId = paciente.PacienteId, .Alergias = "Latex"}

        Dim resultado = Await _service.Guardar(modificado)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of HistorialClinico)()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual("Latex", guardados(0).Alergias)
    End Function

    ' FechaActualizacion es "última modificación": cada guardado la renueva.
    <TestMethod>
    Public Async Function Guardar_HistorialExistente_RenuevaLaFechaDeActualizacion() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim original = SembrarHistorial(Factory, paciente.PacienteId)
        Dim modificado As New HistorialClinico With {
            .HistoriaId = original.HistoriaId, .PacienteId = paciente.PacienteId, .Alergias = "Latex"}

        Await _service.Guardar(modificado)

        Assert.IsTrue(LeerTodos(Of HistorialClinico)()(0).FechaActualizacion > FechaBase)
    End Function

    ' REGLA PENDIENTE: un paciente solo puede tener UN historial (índice único).
    ' Hoy el segundo Guardar lanza DbUpdateException.
    <TestMethod>
    Public Async Function Guardar_SegundoHistorialDelMismoPaciente_RetornaFalloSinLanzarExcepcion() As Task
        Dim paciente = SembrarPaciente(Factory)
        SembrarHistorial(Factory, paciente.PacienteId)
        Dim segundo As New HistorialClinico With {.PacienteId = paciente.PacienteId, .Alergias = "Otra"}

        Dim intento = Await Intentar(Function() _service.Guardar(segundo))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of HistorialClinico)())
    End Function

    ' REGLA PENDIENTE: hoy lanza DbUpdateException (FK_HistorialClinico_Pacientes).
    <TestMethod>
    Public Async Function Guardar_PacienteInexistente_RetornaFalloSinLanzarExcepcion() As Task
        Dim historial As New HistorialClinico With {.PacienteId = 999}

        Dim intento = Await Intentar(Function() _service.Guardar(historial))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of HistorialClinico)())
    End Function

    <TestMethod>
    Public Async Function Guardar_SinPaciente_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New HistorialClinico With {.Alergias = "Ninguna"})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of HistorialClinico)())
    End Function

    ' Guardar con un ID que no existe no debe intentar insertarlo.
    <TestMethod>
    Public Async Function Guardar_HistoriaIdInexistente_RetornaFallo() As Task
        Dim paciente = SembrarPaciente(Factory)
        Dim historial As New HistorialClinico With {.HistoriaId = 999, .PacienteId = paciente.PacienteId}

        Dim intento = Await Intentar(Function() _service.Guardar(historial))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of HistorialClinico)())
    End Function

    ' Un historial no se puede pasar de un paciente a otro.
    <TestMethod>
    Public Async Function Guardar_CambiarDePaciente_RetornaFallo() As Task
        Dim p1 = SembrarPaciente(Factory)
        Dim p2 = SembrarPaciente(Factory)
        Dim original = SembrarHistorial(Factory, p1.PacienteId)
        Dim movido As New HistorialClinico With {.HistoriaId = original.HistoriaId, .PacienteId = p2.PacienteId}

        Dim resultado = Await _service.Guardar(movido)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(p1.PacienteId, LeerTodos(Of HistorialClinico)()(0).PacienteId)
    End Function

    ' Si el JSON trae el Paciente anidado (como lo devuelve Buscar), no debe modificarlo.
    <TestMethod>
    Public Async Function Guardar_ConPacienteAnidado_NoDebeModificarAlPaciente() As Task
        Dim paciente = SembrarPaciente(Factory, nombre:="Ana")
        Dim original = SembrarHistorial(Factory, paciente.PacienteId)
        Dim modificado As New HistorialClinico With {
            .HistoriaId = original.HistoriaId, .PacienteId = paciente.PacienteId, .Alergias = "Latex",
            .Paciente = New Paciente With {
                .PacienteId = paciente.PacienteId, .Nombre = "Alterado", .Apellido = paciente.Apellido,
                .Cedula = paciente.Cedula, .FechaRegistro = paciente.FechaRegistro}}

        Await _service.Guardar(modificado)

        Using contexto = Factory.CreateDbContext()
            Assert.AreEqual("Ana", contexto.Pacientes.Find(paciente.PacienteId).Nombre,
                            "Guardar un historial no debe cambiar al paciente.")
        End Using
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyeAlPaciente() As Task
        Dim paciente = SembrarPaciente(Factory, nombre:="Ana")
        Dim historial = SembrarHistorial(Factory, paciente.PacienteId)

        Dim resultado = Await _service.Buscar(historial.HistoriaId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Ana", resultado.Value.Paciente.Nombre)
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
        Dim historial = SembrarHistorial(Factory, paciente.PacienteId)
        Dim resultado = Await _service.Buscar(historial.HistoriaId)
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
        SembrarHistorial(Factory, ana.PacienteId, "Penicilina")
        SembrarHistorial(Factory, luis.PacienteId, "Latex")

        Dim porNombre = Await _service.BuscarPorNombre("Ana")
        Dim porApellido = Await _service.BuscarPorNombre("Mota")

        Assert.AreEqual(1, porNombre.Value.Count)
        Assert.AreEqual("Penicilina", porNombre.Value(0).Alergias)
        Assert.AreEqual(1, porApellido.Value.Count)
        Assert.AreEqual("Latex", porApellido.Value(0).Alergias)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_SinCoincidencias_RetornaListaVacia() As Task
        Dim paciente = SembrarPaciente(Factory, nombre:="Ana")
        SembrarHistorial(Factory, paciente.PacienteId)

        Dim resultado = Await _service.BuscarPorNombre("Zzz")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorCedula_RetornaSoloElHistorialDeEsaCedula() As Task
        Dim ana = SembrarPaciente(Factory, cedula:="001-1111111-1")
        Dim luis = SembrarPaciente(Factory, cedula:="001-2222222-2")
        Dim historialAna = SembrarHistorial(Factory, ana.PacienteId)
        SembrarHistorial(Factory, luis.PacienteId)

        Dim resultado = Await _service.BuscarPorCedula("001-1111111-1")

        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual(historialAna.HistoriaId, resultado.Value(0).HistoriaId)
    End Function

#End Region

#Region "GetList"

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraPorAlergias() As Task
        SembrarHistorial(Factory, SembrarPaciente(Factory).PacienteId, "Penicilina")
        SembrarHistorial(Factory, SembrarPaciente(Factory).PacienteId, "Latex")

        Dim resultado = Await _service.GetList(Function(h) h.Alergias = "Latex")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(1, resultado.Value.Count)
        Assert.IsNotNull(resultado.Value(0).Paciente)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_HistorialExistente_LoQuitaYConservaLosDemas() As Task
        Dim a = SembrarHistorial(Factory, SembrarPaciente(Factory).PacienteId)
        Dim b = SembrarHistorial(Factory, SembrarPaciente(Factory).PacienteId)

        Dim resultado = Await _service.Eliminar(a.HistoriaId)

        Assert.IsTrue(resultado.IsSuccess)
        Dim quedan = LeerTodos(Of HistorialClinico)()
        Assert.AreEqual(1, quedan.Count)
        Assert.AreEqual(b.HistoriaId, quedan(0).HistoriaId)
    End Function

    ' Borrar algo que no existe debe reportar fallo, no éxito.
    <TestMethod>
    Public Async Function Eliminar_IdInexistente_RetornaFalloYNoAfectaOtrosRegistros() As Task
        SembrarHistorial(Factory, SembrarPaciente(Factory).PacienteId)

        Dim resultado = Await _service.Eliminar(999)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of HistorialClinico)())
    End Function

#End Region

End Class
