Imports GestionCirugiaPlastica.Application.Services
Imports GestionCirugiaPlastica.Infrastructure.Models
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class RolesServiceTests
    Inherits PruebaBase

    Private _service As RolesServices

    <TestInitialize>
    Public Sub Iniciar()
        _service = New RolesServices(Factory)
    End Sub

#Region "Guardar - rol nuevo"

    <TestMethod>
    Public Async Function Guardar_RolNuevo_LoInsertaRecortandoEspacios() As Task
        Dim resultado = Await _service.Guardar(New Role With {
            .Nombre = " Enfermera ", .Descripcion = " Personal de enfermeria "})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Role)()
        Assert.AreEqual(1, guardados.Count)
        Assert.IsTrue(guardados(0).RolId > 0, "La base debe generar el Id.")
        Assert.AreEqual("Enfermera", guardados(0).Nombre)
        Assert.AreEqual("Personal de enfermeria", guardados(0).Descripcion)
    End Function

    <TestMethod>
    Public Async Function Guardar_DescripcionVaciaOEnBlanco_SeGuardaComoNula() As Task
        Dim sinDescripcion = Await _service.Guardar(New Role With {.Nombre = "Uno"})
        Dim enBlanco = Await _service.Guardar(New Role With {.Nombre = "Dos", .Descripcion = "   "})

        Assert.IsTrue(sinDescripcion.IsSuccess)
        Assert.IsTrue(enBlanco.IsSuccess)
        For Each rol In LeerTodos(Of Role)()
            Assert.IsNull(rol.Descripcion)
        Next
    End Function

    <TestMethod>
    Public Async Function Guardar_Nulo_RetornaFalloSinLanzarExcepcion() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(Nothing))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
    End Function

    <TestMethod>
    Public Async Function Guardar_NombreVacio_RetornaFallo() As Task
        Dim nulo = Await _service.Guardar(New Role())
        Dim vacio = Await _service.Guardar(New Role With {.Nombre = ""})
        Dim espacios = Await _service.Guardar(New Role With {.Nombre = "   "})

        Assert.IsFalse(nulo.IsSuccess)
        Assert.IsFalse(vacio.IsSuccess)
        Assert.IsFalse(espacios.IsSuccess)
        Assert.AreEqual(0, Contar(Of Role)())
    End Function

    <TestMethod>
    Public Async Function Guardar_NombreDe251Caracteres_RetornaFalloYDe250SeAcepta() As Task
        Dim demasiado = Await _service.Guardar(New Role With {.Nombre = New String("a"c, 251)})
        Dim limite = Await _service.Guardar(New Role With {.Nombre = New String("a"c, 250)})

        Assert.IsFalse(demasiado.IsSuccess)
        Assert.IsTrue(limite.IsSuccess)
        Assert.AreEqual(1, Contar(Of Role)())
    End Function

    <TestMethod>
    Public Async Function Guardar_DescripcionDe251Caracteres_RetornaFalloYDe250SeAcepta() As Task
        Dim demasiado = Await _service.Guardar(New Role With {.Nombre = "Uno", .Descripcion = New String("a"c, 251)})
        Dim limite = Await _service.Guardar(New Role With {.Nombre = "Dos", .Descripcion = New String("a"c, 250)})

        Assert.IsFalse(demasiado.IsSuccess)
        Assert.IsTrue(limite.IsSuccess)
        Assert.AreEqual(1, Contar(Of Role)())
    End Function

    ' El nombre no puede repetirse, sin importar mayúsculas ni espacios sobrantes.
    <TestMethod>
    Public Async Function Guardar_NombreDuplicado_RetornaFalloYNoInserta() As Task
        SembrarRol(Factory, "Enfermera")

        Dim resultado = Await _service.Guardar(New Role With {.Nombre = "  enfermera "})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Role)())
    End Function

#End Region

#Region "Guardar - rol existente"

    <TestMethod>
    Public Async Function Guardar_RolExistente_ActualizaLosDatosSinDuplicar() As Task
        Dim original = SembrarRol(Factory, "Enfermera", "Descripcion vieja")

        Dim resultado = Await _service.Guardar(New Role With {
            .RolId = original.RolId, .Nombre = " Asistente ", .Descripcion = " Descripcion nueva "})

        Assert.IsTrue(resultado.IsSuccess)
        Dim guardados = LeerTodos(Of Role)()
        Assert.AreEqual(1, guardados.Count)
        Assert.AreEqual("Asistente", guardados(0).Nombre)
        Assert.AreEqual("Descripcion nueva", guardados(0).Descripcion)
    End Function

    ' Editar solo la descripción, dejando el mismo nombre, no cuenta como duplicado.
    <TestMethod>
    Public Async Function Guardar_RolExistente_ConSuMismoNombre_NoEsDuplicado() As Task
        Dim original = SembrarRol(Factory, "Enfermera", "Vieja")

        Dim resultado = Await _service.Guardar(New Role With {
            .RolId = original.RolId, .Nombre = "Enfermera", .Descripcion = "Nueva"})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual("Nueva", LeerTodos(Of Role)()(0).Descripcion)
    End Function

    <TestMethod>
    Public Async Function Guardar_RolExistente_ConElNombreDeOtro_RetornaFallo() As Task
        SembrarRol(Factory, "Enfermera")
        Dim otro = SembrarRol(Factory, "Recepcion")

        Dim resultado = Await _service.Guardar(New Role With {.RolId = otro.RolId, .Nombre = "ENFERMERA"})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.IsTrue(LeerTodos(Of Role)().Any(Function(r) r.Nombre = "Recepcion"), "El nombre original no debe cambiar.")
    End Function

    ' Guardar con un ID que no existe no debe intentar insertarlo.
    <TestMethod>
    Public Async Function Guardar_IdInexistente_RetornaFallo() As Task
        Dim intento = Await Intentar(Function() _service.Guardar(New Role With {.RolId = 999, .Nombre = "Fantasma"}))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(0, Contar(Of Role)())
    End Function

    <TestMethod>
    Public Async Function Guardar_RenombrarAlAdministrador_RetornaFalloYConservaElNombre() As Task
        Dim admin = SembrarRol(Factory, "Administrador")

        Dim resultado = Await _service.Guardar(New Role With {.RolId = admin.RolId, .Nombre = "Jefe"})

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual("Administrador", LeerTodos(Of Role)()(0).Nombre)
    End Function

    ' El administrador sí puede cambiar su descripción, siempre que conserve el nombre.
    <TestMethod>
    Public Async Function Guardar_AdministradorConSuNombre_PuedeCambiarLaDescripcion() As Task
        Dim admin = SembrarRol(Factory, "Administrador", "Vieja")

        Dim resultado = Await _service.Guardar(New Role With {
            .RolId = admin.RolId, .Nombre = "Administrador", .Descripcion = "Acceso total"})

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual("Acceso total", LeerTodos(Of Role)()(0).Descripcion)
    End Function

#End Region

#Region "Buscar"

    <TestMethod>
    Public Async Function Buscar_IdExistente_RetornaElRol() As Task
        Dim rol = SembrarRol(Factory, "Enfermera")

        Dim resultado = Await _service.Buscar(rol.RolId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Enfermera", resultado.Value.Nombre)
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
        SembrarRol(Factory, "Recepcion", "Atiende pacientes")
        SembrarRol(Factory, "Enfermera", "Atiende pacientes")
        SembrarRol(Factory, "Contador", "Maneja pagos")

        Dim resultado = Await _service.GetList(Function(r) r.Descripcion = "Atiende pacientes")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual("Enfermera", resultado.Value(0).Nombre)
        Assert.AreEqual("Recepcion", resultado.Value(1).Nombre)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_CoincidenciaParcial_RetornaLosQueContienenElTexto() As Task
        SembrarRol(Factory, "Enfermera jefe")
        SembrarRol(Factory, "Enfermera auxiliar")
        SembrarRol(Factory, "Recepcion")

        Dim resultado = Await _service.BuscarPorNombre(" nfermera ")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual("Enfermera auxiliar", resultado.Value(0).Nombre)
        Assert.AreEqual("Enfermera jefe", resultado.Value(1).Nombre)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_TextoVacioONulo_RetornaListaVacia() As Task
        SembrarRol(Factory, "Enfermera")

        Dim vacio = Await _service.BuscarPorNombre("")
        Dim espacios = Await _service.BuscarPorNombre("   ")
        Dim nulo = Await _service.BuscarPorNombre(Nothing)

        Assert.AreEqual(0, vacio.Value.Count)
        Assert.AreEqual(0, espacios.Value.Count)
        Assert.AreEqual(0, nulo.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorNombre_SinCoincidencias_RetornaListaVacia() As Task
        SembrarRol(Factory, "Enfermera")

        Dim resultado = Await _service.BuscarPorNombre("xyz")

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(0, resultado.Value.Count)
    End Function

    <TestMethod>
    Public Async Function BuscarPorUsuario_RetornaElRolDeEseUsuario() As Task
        Dim enfermera = SembrarRol(Factory, "Enfermera")
        Dim recepcion = SembrarRol(Factory, "Recepcion")
        SembrarUsuarioConRol(Factory, recepcion.RolId)
        Dim usuario = SembrarUsuarioConRol(Factory, enfermera.RolId)

        Dim resultado = Await _service.BuscarPorUsuario(usuario.UsuarioId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsNotNull(resultado.Value)
        Assert.AreEqual("Enfermera", resultado.Value.Nombre)
    End Function

    <TestMethod>
    Public Async Function BuscarPorUsuario_UsuarioInexistenteOIdInvalido_RetornaValorNulo() As Task
        Dim inexistente = Await _service.BuscarPorUsuario(999)
        Dim cero = Await _service.BuscarPorUsuario(0)

        Assert.IsTrue(inexistente.IsSuccess)
        Assert.IsNull(inexistente.Value)
        Assert.IsTrue(cero.IsSuccess)
        Assert.IsNull(cero.Value)
    End Function

    <TestMethod>
    Public Async Function BuscarUsuarios_RetornaSoloLosDelRolOrdenadosPorNombreYSinContrasena() As Task
        Dim enfermera = SembrarRol(Factory, "Enfermera")
        Dim recepcion = SembrarRol(Factory, "Recepcion")
        SembrarUsuarioConRol(Factory, enfermera.RolId, "Carlos")
        SembrarUsuarioConRol(Factory, enfermera.RolId, "Ana", activo:=False)
        SembrarUsuarioConRol(Factory, recepcion.RolId, "Beto")

        Dim resultado = Await _service.BuscarUsuarios(enfermera.RolId)

        Assert.IsTrue(resultado.IsSuccess)
        Assert.AreEqual(2, resultado.Value.Count)
        Assert.AreEqual("Ana", resultado.Value(0).Nombre)
        Assert.AreEqual("Carlos", resultado.Value(1).Nombre)
        Assert.IsFalse(resultado.Value(0).Activo, "Incluye también a los usuarios inactivos.")
        Assert.IsNull(resultado.Value(0).PasswordHash, "Nunca debe devolver la contraseña.")
        Assert.IsNull(resultado.Value(1).PasswordHash, "Nunca debe devolver la contraseña.")
    End Function

    <TestMethod>
    Public Async Function BuscarUsuarios_RolSinUsuariosOIdInvalido_RetornaListaVacia() As Task
        Dim rol = SembrarRol(Factory, "Enfermera")

        Dim sinUsuarios = Await _service.BuscarUsuarios(rol.RolId)
        Dim invalido = Await _service.BuscarUsuarios(0)

        Assert.IsTrue(sinUsuarios.IsSuccess)
        Assert.AreEqual(0, sinUsuarios.Value.Count)
        Assert.AreEqual(0, invalido.Value.Count)
    End Function

    <TestMethod>
    Public Async Function TieneUsuarios_SegunTengaOno() As Task
        Dim conUsuarios = SembrarRol(Factory, "Enfermera")
        Dim sinUsuarios = SembrarRol(Factory, "Recepcion")
        SembrarUsuarioConRol(Factory, conUsuarios.RolId)

        Dim si = Await _service.TieneUsuarios(conUsuarios.RolId)
        Dim no = Await _service.TieneUsuarios(sinUsuarios.RolId)
        Dim invalido = Await _service.TieneUsuarios(0)

        Assert.IsTrue(si.IsSuccess)
        Assert.IsTrue(si.Value)
        Assert.IsFalse(no.Value)
        Assert.IsFalse(invalido.Value)
    End Function

    <TestMethod>
    Public Async Function ContarUsuarios_RetornaLaCantidadDeEseRol() As Task
        Dim enfermera = SembrarRol(Factory, "Enfermera")
        Dim recepcion = SembrarRol(Factory, "Recepcion")
        SembrarUsuarioConRol(Factory, enfermera.RolId)
        SembrarUsuarioConRol(Factory, enfermera.RolId, activo:=False)
        SembrarUsuarioConRol(Factory, recepcion.RolId)

        Dim cantidad = Await _service.ContarUsuarios(enfermera.RolId)
        Dim ninguno = Await _service.ContarUsuarios(999)
        Dim invalido = Await _service.ContarUsuarios(0)

        Assert.IsTrue(cantidad.IsSuccess)
        Assert.AreEqual(2, cantidad.Value, "Cuenta también a los inactivos.")
        Assert.AreEqual(0, ninguno.Value)
        Assert.AreEqual(0, invalido.Value)
    End Function

#End Region

#Region "ExisteAdministradorActivo"

    <TestMethod>
    Public Async Function ExisteAdministradorActivo_ConUsuarioAdministradorActivo_RetornaVerdadero() As Task
        Dim admin = SembrarRol(Factory, "Administrador")
        SembrarUsuarioConRol(Factory, admin.RolId, activo:=True)

        Dim resultado = Await _service.ExisteAdministradorActivo()

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsTrue(resultado.Value)
    End Function

    <TestMethod>
    Public Async Function ExisteAdministradorActivo_SoloAdministradoresInactivos_RetornaFalso() As Task
        Dim admin = SembrarRol(Factory, "Administrador")
        SembrarUsuarioConRol(Factory, admin.RolId, activo:=False)

        Dim resultado = Await _service.ExisteAdministradorActivo()

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsFalse(resultado.Value)
    End Function

    <TestMethod>
    Public Async Function ExisteAdministradorActivo_SoloUsuariosDeOtroRol_RetornaFalso() As Task
        SembrarRol(Factory, "Administrador")
        Dim enfermera = SembrarRol(Factory, "Enfermera")
        SembrarUsuarioConRol(Factory, enfermera.RolId, activo:=True)

        Dim resultado = Await _service.ExisteAdministradorActivo()

        Assert.IsFalse(resultado.Value)
    End Function

    <TestMethod>
    Public Async Function ExisteAdministradorActivo_SinUsuarios_RetornaFalso() As Task
        Dim resultado = Await _service.ExisteAdministradorActivo()

        Assert.IsTrue(resultado.IsSuccess)
        Assert.IsFalse(resultado.Value)
    End Function

#End Region

#Region "Eliminar"

    <TestMethod>
    Public Async Function Eliminar_SinUsuarios_LoQuitaYConservaLosDemas() As Task
        Dim a = SembrarRol(Factory, "Enfermera")
        Dim b = SembrarRol(Factory, "Recepcion")

        Dim resultado = Await _service.Eliminar(a.RolId)

        Assert.IsTrue(resultado.IsSuccess)
        Dim quedan = LeerTodos(Of Role)()
        Assert.AreEqual(1, quedan.Count)
        Assert.AreEqual(b.RolId, quedan(0).RolId)
    End Function

    ' Con usuarios asignados no se puede borrar: debe fallar, no lanzar excepción de FK.
    <TestMethod>
    Public Async Function Eliminar_ConUsuariosAsignados_RetornaFalloYNoBorra() As Task
        Dim rol = SembrarRol(Factory, "Enfermera")
        SembrarUsuarioConRol(Factory, rol.RolId)

        Dim intento = Await Intentar(Function() _service.Eliminar(rol.RolId))

        Assert.IsNull(intento.Excepcion, $"No debería lanzar: {intento.Excepcion?.GetType().Name}")
        Assert.IsFalse(intento.Resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Role)())
    End Function

    <TestMethod>
    Public Async Function Eliminar_RolAdministrador_RetornaFalloYNoBorra() As Task
        Dim admin = SembrarRol(Factory, "Administrador")

        Dim resultado = Await _service.Eliminar(admin.RolId)

        Assert.IsFalse(resultado.IsSuccess)
        Assert.AreEqual(1, Contar(Of Role)())
    End Function

    <TestMethod>
    Public Async Function Eliminar_IdInexistenteOInvalido_RetornaFalloYNoAfectaOtrosRegistros() As Task
        SembrarRol(Factory, "Enfermera")

        Dim inexistente = Await _service.Eliminar(999)
        Dim cero = Await _service.Eliminar(0)

        Assert.IsFalse(inexistente.IsSuccess)
        Assert.IsFalse(cero.IsSuccess)
        Assert.AreEqual(1, Contar(Of Role)())
    End Function

#End Region

End Class
