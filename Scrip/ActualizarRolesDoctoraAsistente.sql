/*
  Actualiza los roles de la clínica y deja disponibles las cuentas de prueba.
  Después de ejecutar este script, inicia la API una vez: el seeder de Program.cs
  reemplaza el hash temporal de asistente.demo con PasswordHasher<Usuario>.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @DoctorId int = (SELECT TOP (1) RolId FROM dbo.Roles WHERE Nombre = N'Doctor');
DECLARE @DoctoraId int = (SELECT TOP (1) RolId FROM dbo.Roles WHERE Nombre = N'Doctora');

IF @DoctorId IS NOT NULL AND @DoctoraId IS NULL
BEGIN
    UPDATE dbo.Roles
    SET Nombre = N'Doctora', Descripcion = N'Jefa / Dueña; acceso total al sistema'
    WHERE RolId = @DoctorId;
    SET @DoctoraId = @DoctorId;
END
ELSE IF @DoctorId IS NOT NULL AND @DoctoraId IS NOT NULL
BEGIN
    UPDATE dbo.Usuarios SET RolId = @DoctoraId WHERE RolId = @DoctorId;
    DELETE dbo.Roles WHERE RolId = @DoctorId;
END;

IF @DoctoraId IS NULL
BEGIN
    INSERT dbo.Roles (Nombre, Descripcion)
    VALUES (N'Doctora', N'Jefa / Dueña; acceso total al sistema');
    SET @DoctoraId = CONVERT(int, SCOPE_IDENTITY());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'AsistenteClinico')
    INSERT dbo.Roles (Nombre, Descripcion)
    VALUES (N'AsistenteClinico', N'Acceso clínico a expedientes, cirugías y recetas');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Secretaria')
    INSERT dbo.Roles (Nombre, Descripcion)
    VALUES (N'Secretaria', N'Recepción, agenda, pacientes y cobros');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Paciente')
    INSERT dbo.Roles (Nombre, Descripcion)
    VALUES (N'Paciente', N'Acceso al perfil y citas propias');

SET @DoctoraId = (SELECT TOP (1) RolId FROM dbo.Roles WHERE Nombre = N'Doctora');
DECLARE @AsistenteId int = (SELECT TOP (1) RolId FROM dbo.Roles WHERE Nombre = N'AsistenteClinico');

UPDATE dbo.Usuarios
SET RolId = @DoctoraId
WHERE NombreUsuario = N'doctor.demo';

IF EXISTS (SELECT 1 FROM dbo.Usuarios WHERE NombreUsuario = N'asistente.demo')
BEGIN
    UPDATE dbo.Usuarios
    SET RolId = @AsistenteId, Activo = 1
    WHERE NombreUsuario = N'asistente.demo';
END
ELSE
BEGIN
    INSERT dbo.Usuarios
        (FechaRegistro, Nombre, Apellido, Email, NombreUsuario, PacienteId, PasswordHash, RolId, Activo)
    VALUES
        (GETDATE(), N'Asistente', N'Demo', NULL, N'asistente.demo', NULL,
         N'RESET_BY_API_PASSWORD_SEEDER', @AsistenteId, 1);
END;

COMMIT TRANSACTION;
GO

/*
  Credenciales de prueba después de iniciar la API:
  doctor.demo / DoctorTest!2026       => Doctora
  asistente.demo / AsistenteTest!2026 => AsistenteClinico
*/
