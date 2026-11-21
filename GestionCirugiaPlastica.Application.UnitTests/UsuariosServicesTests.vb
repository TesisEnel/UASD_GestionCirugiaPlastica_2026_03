Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class UsuariosServicesTests
    Inherits PruebaBase

    Private Const Clave As String = "ClaveSegura123!"
    Private Const ClaveNueva As String = "OtraClave456789!"

    Private _service As UsuariosServices

    <TestInitialize>
    Public Sub Iniciar()
        _service = New UsuariosServices(Factory)
    End Sub

    ' Crea el usuario a través del service para que tenga un hash REAL de la contraseña.
    Private Async Function CrearConClave(rolId As Integer, Optional email As String = "ana@clinica.test") As Task(Of Usuario)
        Await _service.Guardar(New Usuario With {
            .Nombre = "Ana", .Apellido = "Lopez", .Email = email, .PasswordHash = Clave, .RolId = rolId})
        Dim correo = email.ToLowerInvariant()
        Return LeerTodos(Of Usuario)().First(Function(x) x.Email = correo)
    End Function

#Region "Guardar"

    <TestMethod>
    Public Async Function Guardar_UsuarioNuevo_LoInsertaActivoConCorreoEnMinusculasYClaveProtegida() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario As New Usuario With {
            .Nombre = " Ana ", .Apellido = " Lopez ", .Email = "  Ana@Clinica.TEST ", .PasswordHash = Clave, .RolId = rol.RolId}

        Dim resultado = Await _service.Guardar(usuario)

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Usuario)()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual("Ana", guardados(0).Nombre)
        Assert.AreEqual("Lopez", guardados(0).Apellido)
        Assert.AreEqual("ana@clinica.test", guardados(0).Email)
        Assert.IsTrue(guardados(0).Activo)
        Assert.AreNotEqual(Clave, guardados(0).PasswordHash, "La clave no se guarda en texto plano.")
        Assert.IsTrue(guardados(0).PasswordHash.Length > 20)
    End Function

    <TestMethod>
    Public Async Function Guardar_DatosObligatoriosVacios_RetornaFallo() As Task
        Dim rol = SembrarRolPrueba(Factory)

        Dim r1 = Await _service.Guardar(New Usuario With {.Nombre = " ", .Apellido = "L", .Email = "a@b.com", .PasswordHash = Clave, .RolId = rol.RolId})
        Dim r2 = Await _service.Guardar(New Usuario With {.Nombre = "A", .Apellido = " ", .Email = "a@b.com", .PasswordHash = Clave, .RolId = rol.RolId})
        Dim r3 = Await _service.Guardar(New Usuario With {.Nombre = "A", .Apellido = "L", .Email = " ", .PasswordHash = Clave, .RolId = rol.RolId})

        Assert.IsFalse(r1.IsSuccess, "Nombre")
        Assert.IsFalse(r2.IsSuccess, "Apellido")
        Assert.IsFalse(r3.IsSuccess, "Email")
        Assert.AreEqual(0, Contar(Of Usuario)())
    End Function

    <TestMethod>
    Public Async Function Guardar_CorreoInvalido_RetornaFallo() As Task
        Dim rol = SembrarRolPrueba(Factory)

        Dim resultado = Await _service.Guardar(New Usuario With {
            .Nombre = "A", .Apellido = "L", .Email = "esto-no-es-un-correo", .PasswordHash = Clave, .RolId = rol.RolId})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Usuario)())
    End Function

    <TestMethod>
    Public Async Function Guardar_TextosDeMasDe250Caracteres_RetornaFallo() As Task
        Dim rol = SembrarRolPrueba(Factory)

        Dim resultado = Await _service.Guardar(New Usuario With {
            .Nombre = New String("x"c, 251), .Apellido = "L", .Email = "a@b.com", .PasswordHash = Clave, .RolId = rol.RolId})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Usuario)())
    End Function

    <TestMethod>
    Public Async Function Guardar_CorreoDuplicadoSinImportarMayusculas_RetornaFalloSinLanzarExcepcion() As Task
        Dim rol = SembrarRolPrueba(Factory)
        SembrarUsuarioPrueba(Factory, rol.RolId, "ana@clinica.test")

        Dim intento = Await Intentar(Function() _service.Guardar(New Usuario With {
            .Nombre = "Otra", .Apellido = "Persona", .Email = "ANA@CLINICA.TEST", .PasswordHash = Clave, .RolId = rol.RolId}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Usuario)())
    End Function

    <TestMethod>
    Public Async Function Guardar_ClaveCortaONula_RetornaFallo() As Task
        Dim rol = SembrarRolPrueba(Factory)

        Dim corta = Await _service.Guardar(New Usuario With {.Nombre = "A", .Apellido = "L", .Email = "a@b.com", .PasswordHash = "corta", .RolId = rol.RolId})
        Dim nula = Await _service.Guardar(New Usuario With {.Nombre = "A", .Apellido = "L", .Email = "c@b.com", .RolId = rol.RolId})

        Assert.IsFalse(corta.IsSuccess, "Clave corta")
        Assert.IsFalse(nula.IsSuccess, "Clave nula")
        Assert.AreEqual(0, Contar(Of Usuario)())
    End Function

    <TestMethod>
    Public Async Function Guardar_RolInexistente_RetornaFallo() As Task
        Dim resultado = Await _service.Guardar(New Usuario With {
            .Nombre = "A", .Apellido = "L", .Email = "a@b.com", .PasswordHash = Clave, .RolId = 999})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Usuario)())
    End Function

    <TestMethod>
    Public Async Function Guardar_UsuarioExistente_ActualizaDatosPeroNoClaveNiEstado() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim original = Await CrearConClave(rol.RolId)

        Dim resultado = Await _service.Guardar(New Usuario With {
            .UsuarioId = original.UsuarioId, .Nombre = "Anita", .Apellido = "Gomez",
            .Email = "anita@clinica.test", .RolId = rol.RolId, .PasswordHash = "intento-de-pisar-la-clave", .Activo = False})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardado = LeerTodos(Of Usuario)().Single()
        Assert.AreEqual("Anita", guardado.Nombre)
        Assert.AreEqual("anita@clinica.test", guardado.Email)
        Assert.AreEqual(original.PasswordHash, guardado.PasswordHash, "Guardar no cambia la clave.")
        Assert.IsTrue(guardado.Activo, "Guardar no cambia el estado.")
    End Function

    <TestMethod>
    Public Async Function Guardar_ConservandoSuPropioCorreo_NoChocaConsigoMismo() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim original = Await CrearConClave(rol.RolId)

        Dim resultado = Await _service.Guardar(New Usuario With {
            .UsuarioId = original.UsuarioId, .Nombre = "Nuevo", .Apellido = "Nombre", .Email = original.Email, .RolId = rol.RolId})

        Assert.IsTrue(resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function Guardar_UsuarioIdInexistente_RetornaFallo() As Task
        Dim rol = SembrarRolPrueba(Factory)

        Dim resultado = Await _service.Guardar(New Usuario With {
            .UsuarioId = 999, .Nombre = "A", .Apellido = "L", .Email = "a@b.com", .RolId = rol.RolId})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Usuario)())
    End Function

    ' Guardar se saltaba la protección del último administrador cambiando el RolId.
    <TestMethod>
    Public Async Function Guardar_CambiandoElRolDelUltimoAdministrador_RetornaFalloYConservaElRol() As Task
        Dim admin = SembrarRolPrueba(Factory, "Doctora")
        Dim doctor = SembrarRolPrueba(Factory, "AsistenteClinico")
        Dim usuario = SembrarUsuarioPrueba(Factory, admin.RolId, "jefe@clinica.test")

        Dim resultado = Await _service.Guardar(New Usuario With {
            .UsuarioId = usuario.UsuarioId, .Nombre = usuario.Nombre, .Apellido = usuario.Apellido,
            .Email = usuario.Email, .RolId = doctor.RolId})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(admin.RolId, LeerTodos(Of Usuario)().Single().RolId)
    End Function

#End Region

#Region "Consultas"

    <TestMethod>
    Public Async Function Buscar_IdExistente_IncluyeElRolYNuncaDevuelveLaClave() As Task
        Dim rol = SembrarRolPrueba(Factory, "Doctor")
        Dim usuario = Await CrearConClave(rol.RolId)

        Dim resultado = Await _service.Buscar(usuario.UsuarioId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Doctor", resultado.Value.Rol.Nombre)
        Assert.AreEqual(String.Empty, resultado.Value.PasswordHash)
    End Function

    <TestMethod>
    Public Async Function Buscar_IdInexistenteOInvalido_RetornaValorNulo() As Task
        Dim r1 = Await _service.Buscar(999)
        Dim r2 = Await _service.Buscar(0)

        Assert.IsTrue(r1.IsSuccess)
        Assert.IsNull(r1.Value)
        Assert.IsNull(r2.Value)
    End Function

    ' Usa la misma configuración de JSON que Program.cs (IgnoreCycles).
    <TestMethod>
    Public Async Function Buscar_ResultadoSePuedeSerializarAJson() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = SembrarUsuarioPrueba(Factory, rol.RolId)
        Dim resultado = Await _service.Buscar(usuario.UsuarioId)
        Dim opciones As New JsonSerializerOptions With {.ReferenceHandler = ReferenceHandler.IgnoreCycles}

        Dim excepcion As Exception = Nothing
        Try
            Dim json = JsonSerializer.Serialize(resultado.Value, opciones)
        Catch ex As Exception
            excepcion = ex
        End Try

        Assert.IsNull(excepcion, $"No se pudo serializar: {excepcion?.Message}")
    End Function

    <TestMethod>
    Public Async Function BuscarPorEmail_IgnoraMayusculasYEspacios() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = SembrarUsuarioPrueba(Factory, rol.RolId, "ana@clinica.test")

        Dim resultado = Await _service.BuscarPorEmail("  ANA@Clinica.test ")

        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual(usuario.UsuarioId, resultado.Value.UsuarioId)
    End Function

    <TestMethod>
    Public Async Function BuscarPorEmail_VacioOInexistente_RetornaValorNulo() As Task
        Dim r1 = Await _service.BuscarPorEmail("  ")
        Dim r2 = Await _service.BuscarPorEmail("nadie@clinica.test")

        Assert.IsNull(r1.Value)
        Assert.IsNull(r2.Value)
    End Function

    ' Nota: Contains en SQLite distingue mayúsculas; en SQL Server no. Por eso se usa la misma capitalización.
    <TestMethod>
    Public Async Function BuscarPorNombre_CoincideConNombreApellidoONombreCompleto() As Task
        Dim rol = SembrarRolPrueba(Factory)
        SembrarUsuarioPrueba(Factory, rol.RolId, nombre:="Ana", apellido:="Lopez")
        SembrarUsuarioPrueba(Factory, rol.RolId, nombre:="Luis", apellido:="Mota")

        Dim porNombre = Await _service.BuscarPorNombre("Ana")
        Dim porApellido = Await _service.BuscarPorNombre("Mota")
        Dim porCompleto = Await _service.BuscarPorNombre("Ana Lopez")

        Assert.AreEqual(1, porNombre.Value.Count)
        Assert.AreEqual(1, porApellido.Value.Count)
        Assert.AreEqual(1, porCompleto.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_Vacio_RetornaListaVacia() As Task
        Dim rol = SembrarRolPrueba(Factory)
        SembrarUsuarioPrueba(Factory, rol.RolId)

        Dim resultado = Await _service.BuscarPorNombre("  ")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorRol_RetornaSoloLosDeEseRol() As Task
        Dim r1 = SembrarRolPrueba(Factory, "Doctor")
        Dim r2 = SembrarRolPrueba(Factory, "Secretaria")
        SembrarUsuarioPrueba(Factory, r1.RolId)
        SembrarUsuarioPrueba(Factory, r1.RolId)
        SembrarUsuarioPrueba(Factory, r2.RolId)

        Dim resultado = Await _service.BuscarPorRol(r1.RolId)

        Assert.AreEqual(2, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarActivos_RetornaSoloLosActivos() As Task
        Dim rol = SembrarRolPrueba(Factory)
        SembrarUsuarioPrueba(Factory, rol.RolId)
        SembrarUsuarioPrueba(Factory, rol.RolId, activo:=False)

        Dim resultado = Await _service.BuscarActivos()

        Assert.AreEqual(1, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function GetList_ConCriterio_FiltraYNuncaDevuelveLaClave() As Task
        Dim rol = SembrarRolPrueba(Factory)
        SembrarUsuarioPrueba(Factory, rol.RolId, nombre:="Ana")
        SembrarUsuarioPrueba(Factory, rol.RolId, nombre:="Luis")

        Dim resultado = Await _service.GetList(Function(u) u.Nombre = "Luis")

        Assert.AreEqual(1, resultado.Value.Count)
        Assert.AreEqual(String.Empty, resultado.Value(0).PasswordHash)
    End Function

#End Region

#Region "Cambiar rol"

    <TestMethod>
    Public Async Function CambiarRol_RolValido_LoCambia() As Task
        Dim r1 = SembrarRolPrueba(Factory, "Doctor")
        Dim r2 = SembrarRolPrueba(Factory, "Secretaria")
        Dim usuario = SembrarUsuarioPrueba(Factory, r1.RolId)

        Dim resultado = Await _service.CambiarRol(usuario.UsuarioId, r2.RolId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(r2.RolId, LeerTodos(Of Usuario)().Single().RolId)
    End Function

    <TestMethod>
    Public Async Function CambiarRol_UsuarioORolInexistente_RetornaFallo() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = SembrarUsuarioPrueba(Factory, rol.RolId)

        Assert.IsFalse((Await _service.CambiarRol(999, rol.RolId)).IsSuccess, "Usuario inexistente")
        Assert.IsFalse((Await _service.CambiarRol(usuario.UsuarioId, 999)).IsSuccess, "Rol inexistente")
    End Function

    <TestMethod>
    Public Async Function CambiarRol_AlMismoRol_RetornaFallo() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = SembrarUsuarioPrueba(Factory, rol.RolId)

        Dim resultado = Await _service.CambiarRol(usuario.UsuarioId, rol.RolId)

        Assert.IsFalse(resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function CambiarRol_UltimoAdministradorActivo_RetornaFallo() As Task
        Dim admin = SembrarRolPrueba(Factory, "Doctora")
        Dim doctor = SembrarRolPrueba(Factory, "AsistenteClinico")
        Dim usuario = SembrarUsuarioPrueba(Factory, admin.RolId)

        Dim resultado = Await _service.CambiarRol(usuario.UsuarioId, doctor.RolId)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(admin.RolId, LeerTodos(Of Usuario)().Single().RolId)
    End Function

    <TestMethod>
    Public Async Function CambiarRol_DoctorConOtraDoctoraActiva_LoCambia() As Task
        Dim doctora = SembrarRolPrueba(Factory, "Doctora")
        Dim asistente = SembrarRolPrueba(Factory, "AsistenteClinico")
        Dim u1 = SembrarUsuarioPrueba(Factory, doctora.RolId)
        SembrarUsuarioPrueba(Factory, doctora.RolId)

        Dim resultado = Await _service.CambiarRol(u1.UsuarioId, asistente.RolId)

        Assert.IsTrue(resultado.IsSuccess)
    End Function

#End Region

#Region "Cambiar contraseña"

    <TestMethod>
    Public Async Function CambiarPassword_ClaveActualCorrecta_LaCambia() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = Await CrearConClave(rol.RolId)

        Dim resultado = Await _service.CambiarPassword(usuario.UsuarioId, Clave, ClaveNueva)

        Assert.IsTrue(resultado.IsSuccess)
        ' La clave nueva ya es la vigente y la vieja dejó de servir.
        Assert.IsFalse((Await _service.CambiarPassword(usuario.UsuarioId, Clave, "TercerClave789012!")).IsSuccess, "La clave vieja no debe servir")
        Assert.IsTrue((Await _service.CambiarPassword(usuario.UsuarioId, ClaveNueva, "TercerClave789012!")).IsSuccess, "La clave nueva debe servir")
    End Function

    <TestMethod>
    Public Async Function CambiarPassword_ClaveActualIncorrecta_RetornaFalloYNoLaCambia() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = Await CrearConClave(rol.RolId)

        Dim resultado = Await _service.CambiarPassword(usuario.UsuarioId, "ClaveEquivocada99!", ClaveNueva)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(usuario.PasswordHash, LeerTodos(Of Usuario)().Single().PasswordHash)
    End Function

    <TestMethod>
    Public Async Function CambiarPassword_NuevaCortaOIgualALaActual_RetornaFallo() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = Await CrearConClave(rol.RolId)

        Assert.IsFalse((Await _service.CambiarPassword(usuario.UsuarioId, Clave, "corta")).IsSuccess, "Corta")
        Assert.IsFalse((Await _service.CambiarPassword(usuario.UsuarioId, Clave, Clave)).IsSuccess, "Igual")
        Assert.IsFalse((Await _service.CambiarPassword(usuario.UsuarioId, Clave, "  ")).IsSuccess, "Vacía")
    End Function

    <TestMethod>
    Public Async Function CambiarPassword_UsuarioInactivoOInexistente_RetornaFallo() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = Await CrearConClave(rol.RolId)
        Using contexto = Factory.CreateDbContext()
            contexto.Usuarios.Find(usuario.UsuarioId).Activo = False
            contexto.SaveChanges()
        End Using

        Assert.IsFalse((Await _service.CambiarPassword(usuario.UsuarioId, Clave, ClaveNueva)).IsSuccess, "Inactivo")
        Assert.IsFalse((Await _service.CambiarPassword(999, Clave, ClaveNueva)).IsSuccess, "Inexistente")
    End Function

#End Region

#Region "Activar y desactivar"

    <TestMethod>
    Public Async Function Desactivar_UsuarioActivo_LoDesactiva() As Task
        Dim rol = SembrarRolPrueba(Factory, "Doctor")
        Dim usuario = SembrarUsuarioPrueba(Factory, rol.RolId)

        Dim resultado = Await _service.Desactivar(usuario.UsuarioId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsFalse(LeerTodos(Of Usuario)().Single().Activo)
    End Function

    <TestMethod>
    Public Async Function Desactivar_UltimoAdministradorActivo_RetornaFallo() As Task
        Dim admin = SembrarRolPrueba(Factory, "Administrador")
        Dim usuario = SembrarUsuarioPrueba(Factory, admin.RolId)

        Dim resultado = Await _service.Desactivar(usuario.UsuarioId)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.IsTrue(LeerTodos(Of Usuario)().Single().Activo)
    End Function

    <TestMethod>
    Public Async Function Desactivar_AdministradorConOtroAdministradorActivo_LoDesactiva() As Task
        Dim admin = SembrarRolPrueba(Factory, "Administrador")
        Dim u1 = SembrarUsuarioPrueba(Factory, admin.RolId)
        SembrarUsuarioPrueba(Factory, admin.RolId)

        Dim resultado = Await _service.Desactivar(u1.UsuarioId)

        Assert.IsTrue(resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function Activar_UsuarioInactivo_LoReactiva() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = SembrarUsuarioPrueba(Factory, rol.RolId, activo:=False)

        Dim resultado = Await _service.Activar(usuario.UsuarioId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsTrue(LeerTodos(Of Usuario)().Single().Activo)
    End Function

    <TestMethod>
    Public Async Function ActivarYDesactivar_YaEnEseEstadoOInexistente_RetornanFallo() As Task
        Dim rol = SembrarRolPrueba(Factory, "Doctor")
        Dim activo = SembrarUsuarioPrueba(Factory, rol.RolId)
        Dim inactivo = SembrarUsuarioPrueba(Factory, rol.RolId, activo:=False)

        Assert.IsFalse((Await _service.Activar(activo.UsuarioId)).IsSuccess, "Activar un activo")
        Assert.IsFalse((Await _service.Desactivar(inactivo.UsuarioId)).IsSuccess, "Desactivar un inactivo")
        Assert.IsFalse((Await _service.Activar(999)).IsSuccess, "Activar inexistente")
        Assert.IsFalse((Await _service.Desactivar(999)).IsSuccess, "Desactivar inexistente")
    End Function

#End Region

#Region "Verificar correo y eliminar"

    <TestMethod>
    Public Async Function VerificarEmail_ExisteYExcluyendoAlPropioUsuario() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = SembrarUsuarioPrueba(Factory, rol.RolId, "ana@clinica.test")

        Dim existe = Await _service.VerificarEmail("ANA@clinica.test")
        Dim excluyendoAlPropio = Await _service.VerificarEmail("ana@clinica.test", usuario.UsuarioId)
        Dim noExiste = Await _service.VerificarEmail("nadie@clinica.test")

        Assert.IsTrue(existe.Value)
        Assert.IsFalse(excluyendoAlPropio.Value)
        Assert.IsFalse(noExiste.Value)
    End Function

    ' Los usuarios no se eliminan: debe reportar fallo (no éxito) y conservarlo.
    <TestMethod>
    Public Async Function Eliminar_RetornaFalloYConservaAlUsuario() As Task
        Dim rol = SembrarRolPrueba(Factory)
        Dim usuario = SembrarUsuarioPrueba(Factory, rol.RolId)

        Dim resultado = Await _service.Eliminar(usuario.UsuarioId)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Usuario)())
    End Function

#End Region

End Class
