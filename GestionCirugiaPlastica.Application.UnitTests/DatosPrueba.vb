Imports System.Linq
Imports System.Threading
Imports GestionCirugiaPlastica.Infrastructure.Models

''' <summary>
''' Inserta datos directamente en la base (sin pasar por los services) para
''' preparar el escenario de cada test. Todas las fechas son fijas.
''' </summary>
Public Module DatosPrueba

    Public ReadOnly FechaBase As New DateTime(2026, 3, 10, 9, 30, 0)

    Private _contador As Integer

    Private Function Siguiente() As Integer
        Return Interlocked.Increment(_contador)
    End Function

    Private Function Persistir(Of T As Class)(f As TestDbContextFactory, entidad As T) As T
        Using contexto = f.CreateDbContext()
            contexto.Add(entidad)
            contexto.SaveChanges()
        End Using
        Return entidad
    End Function

    Public Function SembrarPaciente(f As TestDbContextFactory,
                                    Optional nombre As String = "Maria",
                                    Optional apellido As String = "Perez",
                                    Optional cedula As String = Nothing) As Paciente
        If cedula Is Nothing Then cedula = "001-" & Siguiente().ToString("D7") & "-1"
        Return Persistir(f, New Paciente With {
            .Nombre = nombre, .Apellido = apellido, .Cedula = cedula, .FechaRegistro = FechaBase})
    End Function

    Public Function SembrarProcedimiento(f As TestDbContextFactory,
                                         Optional nombre As String = "Rinoplastia",
                                         Optional precio As Decimal = 50000D) As Procedimiento
        Return Persistir(f, New Procedimiento With {
            .Nombre = nombre, .PrecioBase = precio, .FechaRegistro = FechaBase})
    End Function

    Public Function SembrarCirugia(f As TestDbContextFactory, pacienteId As Integer,
                                   procedimientoId As Integer,
                                   Optional estado As String = "Pendiente") As Cirugia
        Return Persistir(f, New Cirugia With {
            .PacienteId = pacienteId, .ProcedimientoId = procedimientoId,
            .FechaCirugia = FechaBase.AddDays(10), .Estado = estado})
    End Function

    ' Paciente + procedimiento + cirugía ya relacionados.
    Public Function SembrarCirugiaCompleta(f As TestDbContextFactory,
                                           Optional cedula As String = Nothing) As Cirugia
        Dim paciente = SembrarPaciente(f, cedula:=cedula)
        Dim procedimiento = SembrarProcedimiento(f, nombre:="Procedimiento " & Siguiente())
        Return SembrarCirugia(f, paciente.PacienteId, procedimiento.ProcedimientoId)
    End Function

    Public Function SembrarCita(f As TestDbContextFactory, pacienteId As Integer,
                                Optional fecha As DateTime = Nothing,
                                Optional citaAnteriorId As Integer? = Nothing) As Cita
        If fecha = DateTime.MinValue Then fecha = FechaBase
        Return Persistir(f, New Cita With {
            .PacienteId = pacienteId, .Fecha = fecha, .Costo = 1500D,
            .Estado = "Pendiente", .EstadoPago = "Pendiente", .CitaAnteriorId = citaAnteriorId})
    End Function

    Public Function SembrarConsulta(f As TestDbContextFactory, pacienteId As Integer,
                                    Optional citaId As Integer? = Nothing) As Consulta
        Return Persistir(f, New Consulta With {
            .PacienteId = pacienteId, .CitaId = citaId, .Fecha = FechaBase, .Motivo = "Control"})
    End Function

    Public Function SembrarControl(f As TestDbContextFactory, cirugiaId As Integer,
                                   Optional fecha As DateTime = Nothing) As ControlesPostoperatorio
        If fecha = DateTime.MinValue Then fecha = FechaBase.AddDays(11)
        Return Persistir(f, New ControlesPostoperatorio With {
            .CirugiaId = cirugiaId, .Fecha = fecha, .DiasPostoperatorio = 1, .Evolucion = "Favorable"})
    End Function

    ' Receta con UN detalle (por defecto "Ibuprofeno"). Se devuelve con su colección
    ' DetallesReceta ya cargada (con los Id generados), para usar receta.DetallesReceta.First().
    ' Indicaciones queda en Nothing a propósito: un test comprueba que Guardar un detalle no la toca.
    Public Function SembrarReceta(f As TestDbContextFactory, pacienteId As Integer,
                                  Optional medicamento As String = "Ibuprofeno") As Receta
        Dim receta As New Receta With {.PacienteId = pacienteId, .Fecha = FechaBase}
        receta.DetallesReceta.Add(New DetallesReceta With {.Medicamento = medicamento})
        Return Persistir(f, receta)
    End Function

    ' Fotografía de una cirugía. Recuerda: el servicio no escribe en disco, solo guarda la ruta.
    Public Function SembrarFotografia(f As TestDbContextFactory, cirugiaId As Integer,
                                      Optional ruta As String = "fotos/antes.jpg",
                                      Optional tipo As String = "Antes") As Fotografia
        Return Persistir(f, New Fotografia With {
            .CirugiaId = cirugiaId, .Fecha = FechaBase, .Tipo = tipo, .RutaArchivo = ruta})
    End Function

    ' Gasto con fecha fija. UsuarioId es opcional (el gasto puede no tener usuario).
    Public Function SembrarGasto(f As TestDbContextFactory,
                                 Optional concepto As String = "Insumos",
                                 Optional fecha As DateTime = Nothing,
                                 Optional monto As Decimal = 100D,
                                 Optional usuarioId As Integer? = Nothing) As Gasto
        If fecha = DateTime.MinValue Then fecha = FechaBase
        Return Persistir(f, New Gasto With {
            .Fecha = fecha, .Concepto = concepto, .Monto = monto, .UsuarioId = usuarioId})
    End Function

    ' Usuario con su rol (el email es único, por eso lleva contador).
    Public Function SembrarUsuario(f As TestDbContextFactory,
                                   Optional nombre As String = "Laura") As Usuario
        Dim n = Siguiente()
        Dim rol = Persistir(f, New Role With {.Nombre = "Rol " & n})
        Return Persistir(f, New Usuario With {
            .Nombre = nombre, .Apellido = "Gomez", .Email = "usuario" & n & "@clinica.test",
            .PasswordHash = "hash-de-prueba", .RolId = rol.RolId, .FechaRegistro = FechaBase,
            .Activo = True})
    End Function

    ' Historial clínico de un paciente (solo puede haber UNO por paciente: índice único).
    ' FechaActualizacion queda en FechaBase a propósito: un test comprueba que Guardar la renueva.
    Public Function SembrarHistorial(f As TestDbContextFactory, pacienteId As Integer,
                                     Optional alergias As String = "Ninguna") As HistorialClinico
        Return Persistir(f, New HistorialClinico With {
            .PacienteId = pacienteId, .FechaActualizacion = FechaBase, .Alergias = alergias,
            .Antecedentes = "Sin antecedentes", .Medicamentos = "Ninguno", .NotasMedicas = "Sin notas"})
    End Function

    ' Registro del historial de estados de una cita. Se inserta directo, sin pasar por el service.
    Public Function SembrarHistorialEstado(f As TestDbContextFactory, citaId As Integer,
                                           Optional anterior As String = Nothing,
                                           Optional nuevo As String = "Pendiente",
                                           Optional fecha As DateTime = Nothing,
                                           Optional usuarioId As Integer? = Nothing) As HistorialEstadosCitas
        If fecha = DateTime.MinValue Then fecha = FechaBase
        Return Persistir(f, New HistorialEstadosCitas With {
            .CitaId = citaId, .FechaCambio = fecha, .EstadoAnterior = anterior,
            .EstadoNuevo = nuevo, .UsuarioId = usuarioId})
    End Function

    ' Seguro (aseguradora). Activo se pone explícito: igual que con los usuarios, no se puede dejar al default.
    Public Function SembrarSeguro(f As TestDbContextFactory,
                                  Optional nombre As String = Nothing,
                                  Optional activo As Boolean = True) As Seguro
        If nombre Is Nothing Then nombre = "Seguro " & Siguiente()
        Return Persistir(f, New Seguro With {
            .Nombre = nombre, .Activo = activo, .FechaRegistro = FechaBase})
    End Function

    ' Seguro de un paciente. La fecha de registro es fija para poder probar el orden.
    Public Function SembrarPacienteSeguro(f As TestDbContextFactory, pacienteId As Integer, seguroId As Integer,
                                          Optional poliza As String = "POL-001",
                                          Optional fecha As DateTime = Nothing) As PacienteSeguro
        If fecha = DateTime.MinValue Then fecha = FechaBase
        Return Persistir(f, New PacienteSeguro With {
            .PacienteId = pacienteId, .SeguroId = seguroId, .NumeroPoliza = poliza,
            .Titular = "Titular de prueba", .FechaRegistro = fecha})
    End Function

    ' Pago de UNA cirugía o de UNA cita (nunca de las dos). Fecha fija para poder probar búsquedas por fecha.
    Public Function SembrarPago(f As TestDbContextFactory,
                                Optional cirugiaId As Integer? = Nothing,
                                Optional citaId As Integer? = Nothing,
                                Optional monto As Decimal = 1000D,
                                Optional fecha As DateTime = Nothing,
                                Optional metodo As String = "Efectivo") As Pago
        If fecha = DateTime.MinValue Then fecha = FechaBase
        Return Persistir(f, New Pago With {
            .CirugiaId = cirugiaId, .CitaId = citaId, .Monto = monto,
            .Fecha = fecha, .MetodoPago = metodo, .Concepto = "Abono"})
    End Function

    ' Evaluación preoperatoria de una cirugía. Se inserta directo, así que puede quedar
    ' "autorizada" con requisitos vacíos (útil para probar BuscarRequisitosPendientes).
    Public Function SembrarPreoperatorio(f As TestDbContextFactory, cirugiaId As Integer,
                                         Optional autorizado As Boolean = False,
                                         Optional fecha As DateTime = Nothing,
                                         Optional laboratorio As String = "Hemograma normal",
                                         Optional cardiologica As String = "Normal",
                                         Optional anestesica As String = "ASA I",
                                         Optional observaciones As String = "Sin observaciones") As Preoperatorio
        If fecha = DateTime.MinValue Then fecha = FechaBase
        Return Persistir(f, New Preoperatorio With {
            .CirugiaId = cirugiaId, .FechaEvaluacion = fecha, .Autorizado = autorizado,
            .AnalisisLaboratorio = laboratorio, .EvaluacionCardiologica = cardiologica,
            .EvaluacionAnestesica = anestesica, .Observaciones = observaciones})
    End Function

    ' Receta con UN detalle, eligiendo origen, usuario, fecha e indicaciones.
    ' Se devuelve con DetallesReceta cargada (con Id generado), igual que SembrarReceta.
    Public Function SembrarRecetaDetallada(f As TestDbContextFactory, pacienteId As Integer,
                                           Optional consultaId As Integer? = Nothing,
                                           Optional cirugiaId As Integer? = Nothing,
                                           Optional usuarioId As Integer? = Nothing,
                                           Optional fecha As DateTime = Nothing,
                                           Optional medicamento As String = "Ibuprofeno",
                                           Optional indicaciones As String = Nothing) As Receta
        If fecha = DateTime.MinValue Then fecha = FechaBase
        Dim receta As New Receta With {
            .PacienteId = pacienteId, .ConsultaId = consultaId, .CirugiaId = cirugiaId,
            .UsuarioId = usuarioId, .Fecha = fecha, .Indicaciones = indicaciones}
        receta.DetallesReceta.Add(New DetallesReceta With {.Medicamento = medicamento})
        Return Persistir(f, receta)
    End Function

    ' Rol con nombre (único) y descripción opcional.
    Public Function SembrarRol(f As TestDbContextFactory, nombre As String,
                               Optional descripcion As String = Nothing) As Role
        Return Persistir(f, New Role With {.Nombre = nombre, .Descripcion = descripcion})
    End Function

    ' Usuario de un rol que ya existe (SembrarUsuario crea su propio rol, aquí lo eliges tú).
    ' Para dejarlo inactivo se hace un UPDATE después de insertarlo.
    Public Function SembrarUsuarioConRol(f As TestDbContextFactory, rolId As Integer,
                                         Optional nombre As String = "Laura",
                                         Optional activo As Boolean = True) As Usuario
        Dim n = Siguiente()
        Dim usuario = Persistir(f, New Usuario With {
            .Nombre = nombre, .Apellido = "Gomez", .Email = "usuario" & n & "@clinica.test",
            .PasswordHash = "hash-de-prueba", .RolId = rolId, .FechaRegistro = FechaBase, .Activo = True})

        If Not activo Then
            Using contexto = f.CreateDbContext()
                contexto.Usuarios.Find(usuario.UsuarioId).Activo = False
                contexto.SaveChanges()
            End Using
            usuario.Activo = False
        End If

        Return usuario
    End Function

    ' Póliza de un paciente. vence = Nothing significa "sin fecha de vencimiento".
    Public Function SembrarPoliza(f As TestDbContextFactory, pacienteId As Integer, seguroId As Integer,
                                  Optional numero As String = "POL-1",
                                  Optional vence As DateOnly? = Nothing,
                                  Optional fechaRegistro As DateTime = Nothing) As PacienteSeguro
        If fechaRegistro = DateTime.MinValue Then fechaRegistro = FechaBase
        Return Persistir(f, New PacienteSeguro With {
            .PacienteId = pacienteId, .SeguroId = seguroId, .NumeroPoliza = numero,
            .FechaVencimiento = vence, .FechaRegistro = fechaRegistro})
    End Function

    Public Function SembrarRolPrueba(f As TestDbContextFactory,
                                 Optional nombre As String = Nothing) As Role
        Return Persistir(f, New Role With {.Nombre = If(nombre, "Rol " & Siguiente())})
    End Function

    ' OJO: Usuario.Activo tiene HasDefaultValue(true). Si se inserta con Activo = False,
    ' EF omite la columna y la base guarda True. Por eso se inserta y luego se actualiza.
    Public Function SembrarUsuarioPrueba(f As TestDbContextFactory, rolId As Integer,
                                         Optional email As String = Nothing,
                                         Optional activo As Boolean = True,
                                         Optional nombre As String = "Ana",
                                         Optional apellido As String = "Lopez") As Usuario
        Dim usuario = Persistir(f, New Usuario With {
            .Nombre = nombre, .Apellido = apellido,
            .Email = If(email, "usuario" & Siguiente() & "@clinica.test"),
            .PasswordHash = "hash-de-prueba", .RolId = rolId, .FechaRegistro = FechaBase,
            .Activo = True})

        If Not activo Then
            Using c = f.CreateDbContext()
                c.Usuarios.Find(usuario.UsuarioId).Activo = False
                c.SaveChanges()
            End Using
            usuario.Activo = False
        End If

        Return usuario
    End Function

End Module