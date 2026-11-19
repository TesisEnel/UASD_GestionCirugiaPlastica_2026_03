/*
  Parte 1: prepara Usuarios para login mediante username/cédula/teléfono y
  siembra roles y usuarios de prueba. Ejecutar en GestionCirugiaPlastica.
  Contraseñas de prueba listadas al final del script.
*/
IF COL_LENGTH(N'dbo.Usuarios', N'NombreUsuario') IS NULL
    ALTER TABLE dbo.Usuarios ADD NombreUsuario nvarchar(100) NULL;
GO
IF COL_LENGTH(N'dbo.Usuarios', N'PacienteId') IS NULL
    ALTER TABLE dbo.Usuarios ADD PacienteId int NULL;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Usuarios') AND name = N'UQ__Usuarios__A9D10534CD4693E6')
    DROP INDEX UQ__Usuarios__A9D10534CD4693E6 ON dbo.Usuarios;
GO
ALTER TABLE dbo.Usuarios ALTER COLUMN Email nvarchar(250) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Usuarios') AND name = N'UX_Usuarios_Email_NotNull')
    CREATE UNIQUE INDEX UX_Usuarios_Email_NotNull ON dbo.Usuarios(Email) WHERE Email IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Usuarios') AND name = N'UX_Usuarios_NombreUsuario_NotNull')
    CREATE UNIQUE INDEX UX_Usuarios_NombreUsuario_NotNull ON dbo.Usuarios(NombreUsuario) WHERE NombreUsuario IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Usuarios_Pacientes')
    ALTER TABLE dbo.Usuarios ADD CONSTRAINT FK_Usuarios_Pacientes
        FOREIGN KEY (PacienteId) REFERENCES dbo.Pacientes(PacienteId);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Doctora')
    INSERT dbo.Roles (Nombre, Descripcion) VALUES (N'Doctora', N'Jefa / Dueña; acceso total al sistema');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'AsistenteClinico')
    INSERT dbo.Roles (Nombre, Descripcion) VALUES (N'AsistenteClinico', N'Acceso clínico a expedientes, cirugías y recetas');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Secretaria')
    INSERT dbo.Roles (Nombre, Descripcion) VALUES (N'Secretaria', N'Recepción, agenda, pacientes y cobros');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Paciente')
    INSERT dbo.Roles (Nombre, Descripcion) VALUES (N'Paciente', N'Acceso al perfil propio');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE NombreUsuario = N'doctor.demo')
    INSERT dbo.Usuarios (FechaRegistro, Nombre, Apellido, Email, NombreUsuario, PasswordHash, RolId, Activo)
    SELECT GETDATE(), N'Doctora', N'Demo', NULL, N'doctor.demo',
        N'AQAAAAEAAYagAAAAEH8ryt/hvDWmOG0PLEJVOMYc0jKeTEjS8wT2wKxQJauNnl6wUdwIW6z7DI1XgBgdJQ==', RolId, 1
    FROM dbo.Roles WHERE Nombre = N'Doctora';

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE NombreUsuario = N'secretaria.demo')
    INSERT dbo.Usuarios (FechaRegistro, Nombre, Apellido, Email, NombreUsuario, PasswordHash, RolId, Activo)
    SELECT GETDATE(), N'Secretaria', N'Demo', NULL, N'secretaria.demo',
        N'AQAAAAEAAYagAAAAEM3WNoKBln0sPpISOznteHitMxa4HPKGzMbuX8Ru/dtjRJSlANjMCm+iUgOnH0iJAw==', RolId, 1
    FROM dbo.Roles WHERE Nombre = N'Secretaria';

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE NombreUsuario = N'asistente.demo')
    INSERT dbo.Usuarios (FechaRegistro, Nombre, Apellido, Email, NombreUsuario, PasswordHash, RolId, Activo)
    SELECT GETDATE(), N'Asistente', N'Demo', NULL, N'asistente.demo',
        N'RESET_BY_API_PASSWORD_SEEDER', RolId, 1
    FROM dbo.Roles WHERE Nombre = N'AsistenteClinico';

IF NOT EXISTS (SELECT 1 FROM dbo.Pacientes WHERE Cedula = N'00000000001')
    INSERT dbo.Pacientes (FechaRegistro, Nombre, Apellido, Cedula, Telefono, Email)
    VALUES (GETDATE(), N'Paciente', N'Demo', N'00000000001', N'8090000001', NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE NombreUsuario = N'paciente.demo')
    INSERT dbo.Usuarios (FechaRegistro, Nombre, Apellido, Email, NombreUsuario, PacienteId, PasswordHash, RolId, Activo)
    SELECT GETDATE(), N'Paciente', N'Demo', NULL, N'paciente.demo', p.PacienteId,
        N'AQAAAAEAAYagAAAAEI/ciALrrPI22jfj+gknvsJSqQcChgOYuD3n2s01PEJWsWVcAECVbdDGZLSX4pZQ0Q==', r.RolId, 1
    FROM dbo.Pacientes p CROSS JOIN dbo.Roles r
    WHERE p.Cedula = N'00000000001' AND r.Nombre = N'Paciente';
GO

-- Repara la cuenta demo si ya existía sin vinculación cuando se ejecutó la semilla.
UPDATE u
SET u.PacienteId = p.PacienteId
FROM dbo.Usuarios u
CROSS JOIN dbo.Pacientes p
WHERE u.NombreUsuario = N'paciente.demo'
  AND u.PacienteId IS NULL
  AND p.Cedula = N'00000000001';
GO

/* Acceso de prueba (se puede ingresar con username o username/cédula/teléfono para Paciente):
   doctor.demo     / DoctorTest!2026 (rol Doctora)
   asistente.demo  / AsistenteTest!2026
   secretaria.demo / SecretariaTest!2026
   paciente.demo   / PacienteTest!2026
   paciente.demo también puede ingresar con cédula 00000000001 o teléfono 8090000001.
*/
