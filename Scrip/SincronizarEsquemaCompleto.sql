/*
   Sincronización aditiva del esquema GestionCirugiaPlastica.
   Ejecutar en SQL Server Express. No elimina ni recrea datos.
   El archivo inicial crea la base de datos; los scripts posteriores aportan
   únicamente deltas. Este archivo conserva las tablas base existentes y aplica
   los deltas de modelo que aparecen en los scripts y entidades actuales.
*/
USE [GestionCirugiaPlastica];
GO
SET XACT_ABORT ON;
GO

/* Tablas cuyo nombre físico debe conservarse en singular por compatibilidad. */
IF OBJECT_ID(N'dbo.AuditoriaExpediente', N'U') IS NULL
   AND OBJECT_ID(N'dbo.AuditoriaExpedientes', N'U') IS NOT NULL
    EXEC sys.sp_rename N'dbo.AuditoriaExpedientes', N'AuditoriaExpediente';
GO

/* Historial clínico: columnas añadidas después del esquema original. */
IF OBJECT_ID(N'dbo.HistorialClinico', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.HistorialClinico', N'AntecedentesPersonales') IS NULL
        ALTER TABLE dbo.HistorialClinico ADD AntecedentesPersonales nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.HistorialClinico', N'AntecedentesFamiliares') IS NULL
        ALTER TABLE dbo.HistorialClinico ADD AntecedentesFamiliares nvarchar(max) NULL;
END;
GO

/* Pacientes: seguro opcional, con valor inicial seguro para filas existentes. */
IF OBJECT_ID(N'dbo.Pacientes', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Pacientes', N'TieneSeguro') IS NULL
    ALTER TABLE dbo.Pacientes ADD TieneSeguro bit NOT NULL
        CONSTRAINT DF_Pacientes_TieneSeguro DEFAULT (0) WITH VALUES;
GO
IF OBJECT_ID(N'dbo.Pacientes', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Pacientes', N'Sexo') IS NULL
    ALTER TABLE dbo.Pacientes ADD Sexo nvarchar(max) NULL;
GO

/* Citas públicas. */
IF OBJECT_ID(N'dbo.Citas', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Citas', N'CodigoPublico') IS NULL
    ALTER TABLE dbo.Citas ADD CodigoPublico nvarchar(32) NULL;
GO
IF OBJECT_ID(N'dbo.Citas', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Citas') AND name=N'UX_Citas_CodigoPublico')
    CREATE UNIQUE INDEX UX_Citas_CodigoPublico ON dbo.Citas(CodigoPublico) WHERE CodigoPublico IS NOT NULL;
GO

/* Usuarios: login sin correo obligatorio, vinculación opcional y avatar. */
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Usuarios', N'NombreUsuario') IS NULL
        ALTER TABLE dbo.Usuarios ADD NombreUsuario nvarchar(100) NULL;
    IF COL_LENGTH(N'dbo.Usuarios', N'PacienteId') IS NULL
        ALTER TABLE dbo.Usuarios ADD PacienteId int NULL;
    IF COL_LENGTH(N'dbo.Usuarios', N'FotoUrl') IS NULL
        ALTER TABLE dbo.Usuarios ADD FotoUrl nvarchar(500) NULL;
END;
GO
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Usuarios', N'Email') IS NOT NULL
   AND EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Usuarios') AND name=N'Email' AND is_nullable=0)
BEGIN
    IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Usuarios') AND name=N'UX_Usuarios_Email_NotNull')
        DROP INDEX UX_Usuarios_Email_NotNull ON dbo.Usuarios;
    IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Usuarios') AND name=N'UQ__Usuarios__A9D10534CD4693E6')
        DROP INDEX UQ__Usuarios__A9D10534CD4693E6 ON dbo.Usuarios;
    ALTER TABLE dbo.Usuarios ALTER COLUMN Email nvarchar(250) NULL;
END;
GO
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Usuarios', N'NombreUsuario') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Usuarios') AND name=N'UX_Usuarios_NombreUsuario_NotNull')
    CREATE UNIQUE INDEX UX_Usuarios_NombreUsuario_NotNull ON dbo.Usuarios(NombreUsuario) WHERE NombreUsuario IS NOT NULL;
GO
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Usuarios', N'Email') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Usuarios') AND name=N'UX_Usuarios_Email_NotNull')
    CREATE UNIQUE INDEX UX_Usuarios_Email_NotNull ON dbo.Usuarios(Email) WHERE Email IS NOT NULL;
GO

/* Pagos de recepción. */
IF OBJECT_ID(N'dbo.Pagos', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Pagos', N'PacienteId') IS NULL
        ALTER TABLE dbo.Pagos ADD PacienteId int NULL;
    IF COL_LENGTH(N'dbo.Pagos', N'SeguroId') IS NULL
        ALTER TABLE dbo.Pagos ADD SeguroId int NULL;
    IF COL_LENGTH(N'dbo.Pagos', N'MontoSeguro') IS NULL
        ALTER TABLE dbo.Pagos ADD MontoSeguro decimal(12,2) NOT NULL CONSTRAINT DF_Pagos_MontoSeguro DEFAULT(0) WITH VALUES;
    IF COL_LENGTH(N'dbo.Pagos', N'Estado') IS NULL
        ALTER TABLE dbo.Pagos ADD Estado nvarchar(250) NOT NULL CONSTRAINT DF_Pagos_Estado DEFAULT(N'Pagado') WITH VALUES;
END;
GO

/* Documentos clínicos: tamaños y tipos coinciden con DocumentoClinico.cs. */
IF OBJECT_ID(N'dbo.DocumentosClinicos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentosClinicos
    (
        DocumentoClinicoId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentosClinicos PRIMARY KEY,
        PacienteId int NOT NULL,
        CirugiaId int NULL,
        ConsultaId int NULL,
        Categoria nvarchar(40) NOT NULL CONSTRAINT DF_DocumentosClinicos_Categoria DEFAULT(N'Otro'),
        NombreArchivo nvarchar(250) NOT NULL,
        ContentType nvarchar(120) NOT NULL,
        RutaArchivo nvarchar(500) NOT NULL,
        Descripcion nvarchar(500) NULL,
        FechaCarga datetime2 NOT NULL CONSTRAINT DF_DocumentosClinicos_FechaCarga DEFAULT(sysutcdatetime()),
        UsuarioId int NOT NULL
    );
END;
GO
IF OBJECT_ID(N'dbo.DocumentosClinicos', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.DocumentosClinicos', N'PacienteId') IS NULL ALTER TABLE dbo.DocumentosClinicos ADD PacienteId int NULL;
    IF COL_LENGTH(N'dbo.DocumentosClinicos', N'CirugiaId') IS NULL ALTER TABLE dbo.DocumentosClinicos ADD CirugiaId int NULL;
    IF COL_LENGTH(N'dbo.DocumentosClinicos', N'ConsultaId') IS NULL ALTER TABLE dbo.DocumentosClinicos ADD ConsultaId int NULL;
    IF COL_LENGTH(N'dbo.DocumentosClinicos', N'Categoria') IS NULL ALTER TABLE dbo.DocumentosClinicos ADD Categoria nvarchar(40) NOT NULL CONSTRAINT DF_DocumentosClinicos_Categoria_Mig DEFAULT(N'Otro') WITH VALUES;
    IF COL_LENGTH(N'dbo.DocumentosClinicos', N'NombreArchivo') IS NULL ALTER TABLE dbo.DocumentosClinicos ADD NombreArchivo nvarchar(250) NOT NULL CONSTRAINT DF_DocumentosClinicos_NombreArchivo_Mig DEFAULT(N'') WITH VALUES;
    IF COL_LENGTH(N'dbo.DocumentosClinicos', N'ContentType') IS NULL ALTER TABLE dbo.DocumentosClinicos ADD ContentType nvarchar(120) NOT NULL CONSTRAINT DF_DocumentosClinicos_ContentType_Mig DEFAULT(N'application/octet-stream') WITH VALUES;
    IF COL_LENGTH(N'dbo.DocumentosClinicos', N'RutaArchivo') IS NULL ALTER TABLE dbo.DocumentosClinicos ADD RutaArchivo nvarchar(500) NOT NULL CONSTRAINT DF_DocumentosClinicos_RutaArchivo_Mig DEFAULT(N'') WITH VALUES;
    IF COL_LENGTH(N'dbo.DocumentosClinicos', N'Descripcion') IS NULL ALTER TABLE dbo.DocumentosClinicos ADD Descripcion nvarchar(500) NULL;
    IF COL_LENGTH(N'dbo.DocumentosClinicos', N'FechaCarga') IS NULL ALTER TABLE dbo.DocumentosClinicos ADD FechaCarga datetime2 NOT NULL CONSTRAINT DF_DocumentosClinicos_FechaCarga_Mig DEFAULT(sysutcdatetime()) WITH VALUES;
    IF COL_LENGTH(N'dbo.DocumentosClinicos', N'UsuarioId') IS NULL ALTER TABLE dbo.DocumentosClinicos ADD UsuarioId int NULL;
END;
GO

/* Auditoría: BIGINT IDENTITY corresponde a long en AuditoriaExpediente.cs. */
IF OBJECT_ID(N'dbo.AuditoriaExpediente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaExpediente
    (
        AuditoriaExpedienteId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditoriaExpediente PRIMARY KEY,
        PacienteId int NOT NULL,
        UsuarioId int NOT NULL,
        Fecha datetime2 NOT NULL CONSTRAINT DF_AuditoriaExpediente_Fecha DEFAULT(sysutcdatetime()),
        Entidad nvarchar(80) NOT NULL,
        RegistroId int NULL,
        Accion nvarchar(40) NOT NULL,
        Detalle nvarchar(500) NULL
    );
END;
GO
IF OBJECT_ID(N'dbo.AuditoriaExpediente', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.AuditoriaExpediente', N'PacienteId') IS NULL ALTER TABLE dbo.AuditoriaExpediente ADD PacienteId int NULL;
    IF COL_LENGTH(N'dbo.AuditoriaExpediente', N'UsuarioId') IS NULL ALTER TABLE dbo.AuditoriaExpediente ADD UsuarioId int NULL;
    IF COL_LENGTH(N'dbo.AuditoriaExpediente', N'Fecha') IS NULL ALTER TABLE dbo.AuditoriaExpediente ADD Fecha datetime2 NOT NULL CONSTRAINT DF_AuditoriaExpediente_Fecha_Mig DEFAULT(sysutcdatetime()) WITH VALUES;
    IF COL_LENGTH(N'dbo.AuditoriaExpediente', N'Entidad') IS NULL ALTER TABLE dbo.AuditoriaExpediente ADD Entidad nvarchar(80) NOT NULL CONSTRAINT DF_AuditoriaExpediente_Entidad_Mig DEFAULT(N'') WITH VALUES;
    IF COL_LENGTH(N'dbo.AuditoriaExpediente', N'RegistroId') IS NULL ALTER TABLE dbo.AuditoriaExpediente ADD RegistroId int NULL;
    IF COL_LENGTH(N'dbo.AuditoriaExpediente', N'Accion') IS NULL ALTER TABLE dbo.AuditoriaExpediente ADD Accion nvarchar(40) NOT NULL CONSTRAINT DF_AuditoriaExpediente_Accion_Mig DEFAULT(N'') WITH VALUES;
    IF COL_LENGTH(N'dbo.AuditoriaExpediente', N'Detalle') IS NULL ALTER TABLE dbo.AuditoriaExpediente ADD Detalle nvarchar(500) NULL;
END;
GO

/* Seguimiento quirúrgico; los strings sin longitud en EF son nvarchar(max). */
IF OBJECT_ID(N'dbo.Preoperatorios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Preoperatorios
    (
        PreoperatorioId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Preoperatorios PRIMARY KEY,
        FechaEvaluacion datetime NOT NULL CONSTRAINT DF_Preoperatorios_FechaEvaluacion DEFAULT(getdate()),
        CirugiaId int NOT NULL,
        AnalisisLaboratorio nvarchar(max) NULL,
        EvaluacionCardiologica nvarchar(max) NULL,
        EvaluacionAnestesica nvarchar(max) NULL,
        Autorizado bit NOT NULL,
        Observaciones nvarchar(max) NULL
    );
END;
GO
IF OBJECT_ID(N'dbo.ControlesPostoperatorios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ControlesPostoperatorios
    (
        ControlId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ControlesPostoperatorios PRIMARY KEY,
        Fecha datetime NOT NULL,
        CirugiaId int NOT NULL,
        DiasPostoperatorio int NULL,
        Evolucion nvarchar(max) NULL,
        Dolor nvarchar(250) NULL,
        Inflamacion nvarchar(250) NULL,
        Observaciones nvarchar(max) NULL,
        ProximoControl datetime NULL
    );
END;
GO

/* Notificaciones y noticias: dimensiones alineadas con sus entidades. */
IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notificaciones
    (
        NotificacionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notificaciones PRIMARY KEY,
        Tipo nvarchar(50) NOT NULL,
        Titulo nvarchar(200) NOT NULL,
        Mensaje nvarchar(1000) NOT NULL,
        CitaId int NULL,
        FechaCreacion datetime2 NOT NULL CONSTRAINT DF_Notificaciones_FechaCreacion DEFAULT(getdate()),
        Leida bit NOT NULL CONSTRAINT DF_Notificaciones_Leida DEFAULT(0)
    );
END;
GO
IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Notificaciones', N'Tipo') IS NULL ALTER TABLE dbo.Notificaciones ADD Tipo nvarchar(50) NOT NULL CONSTRAINT DF_Notificaciones_Tipo_Mig DEFAULT(N'') WITH VALUES;
    IF COL_LENGTH(N'dbo.Notificaciones', N'Titulo') IS NULL ALTER TABLE dbo.Notificaciones ADD Titulo nvarchar(200) NOT NULL CONSTRAINT DF_Notificaciones_Titulo_Mig DEFAULT(N'') WITH VALUES;
    IF COL_LENGTH(N'dbo.Notificaciones', N'Mensaje') IS NULL ALTER TABLE dbo.Notificaciones ADD Mensaje nvarchar(1000) NOT NULL CONSTRAINT DF_Notificaciones_Mensaje_Mig DEFAULT(N'') WITH VALUES;
    IF COL_LENGTH(N'dbo.Notificaciones', N'CitaId') IS NULL ALTER TABLE dbo.Notificaciones ADD CitaId int NULL;
    IF COL_LENGTH(N'dbo.Notificaciones', N'FechaCreacion') IS NULL ALTER TABLE dbo.Notificaciones ADD FechaCreacion datetime2 NOT NULL CONSTRAINT DF_Notificaciones_FechaCreacion_Mig DEFAULT(getdate()) WITH VALUES;
    IF COL_LENGTH(N'dbo.Notificaciones', N'Leida') IS NULL ALTER TABLE dbo.Notificaciones ADD Leida bit NOT NULL CONSTRAINT DF_Notificaciones_Leida_Mig DEFAULT(0) WITH VALUES;
END;
GO
IF OBJECT_ID(N'dbo.Noticias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Noticias
    (
        NoticiaId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Noticias PRIMARY KEY,
        Titulo nvarchar(200) NOT NULL,
        Resumen nvarchar(500) NULL,
        Contenido nvarchar(max) NOT NULL,
        FechaPublicacion datetime2 NOT NULL CONSTRAINT DF_Noticias_FechaPublicacion DEFAULT(getdate()),
        Publicada bit NOT NULL CONSTRAINT DF_Noticias_Publicada DEFAULT(0),
        CreadaPorUsuarioId int NULL
    );
END;
GO
IF OBJECT_ID(N'dbo.Noticias', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Noticias', N'Titulo') IS NULL ALTER TABLE dbo.Noticias ADD Titulo nvarchar(200) NOT NULL CONSTRAINT DF_Noticias_Titulo_Mig DEFAULT(N'') WITH VALUES;
    IF COL_LENGTH(N'dbo.Noticias', N'Resumen') IS NULL ALTER TABLE dbo.Noticias ADD Resumen nvarchar(500) NULL;
    IF COL_LENGTH(N'dbo.Noticias', N'Contenido') IS NULL ALTER TABLE dbo.Noticias ADD Contenido nvarchar(max) NOT NULL CONSTRAINT DF_Noticias_Contenido_Mig DEFAULT(N'') WITH VALUES;
    IF COL_LENGTH(N'dbo.Noticias', N'FechaPublicacion') IS NULL ALTER TABLE dbo.Noticias ADD FechaPublicacion datetime2 NOT NULL CONSTRAINT DF_Noticias_FechaPublicacion_Mig DEFAULT(getdate()) WITH VALUES;
    IF COL_LENGTH(N'dbo.Noticias', N'Publicada') IS NULL ALTER TABLE dbo.Noticias ADD Publicada bit NOT NULL CONSTRAINT DF_Noticias_Publicada_Mig DEFAULT(0) WITH VALUES;
    IF COL_LENGTH(N'dbo.Noticias', N'CreadaPorUsuarioId') IS NULL ALTER TABLE dbo.Noticias ADD CreadaPorUsuarioId int NULL;
END;
GO

IF OBJECT_ID(N'dbo.Preoperatorios', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Preoperatorios', N'FechaEvaluacion') IS NULL ALTER TABLE dbo.Preoperatorios ADD FechaEvaluacion datetime NOT NULL CONSTRAINT DF_Preoperatorios_FechaEvaluacion_Mig DEFAULT(getdate()) WITH VALUES;
    IF COL_LENGTH(N'dbo.Preoperatorios', N'CirugiaId') IS NULL ALTER TABLE dbo.Preoperatorios ADD CirugiaId int NULL;
    IF COL_LENGTH(N'dbo.Preoperatorios', N'AnalisisLaboratorio') IS NULL ALTER TABLE dbo.Preoperatorios ADD AnalisisLaboratorio nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.Preoperatorios', N'EvaluacionCardiologica') IS NULL ALTER TABLE dbo.Preoperatorios ADD EvaluacionCardiologica nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.Preoperatorios', N'EvaluacionAnestesica') IS NULL ALTER TABLE dbo.Preoperatorios ADD EvaluacionAnestesica nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.Preoperatorios', N'Autorizado') IS NULL ALTER TABLE dbo.Preoperatorios ADD Autorizado bit NOT NULL CONSTRAINT DF_Preoperatorios_Autorizado_Mig DEFAULT(0) WITH VALUES;
    IF COL_LENGTH(N'dbo.Preoperatorios', N'Observaciones') IS NULL ALTER TABLE dbo.Preoperatorios ADD Observaciones nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'dbo.ControlesPostoperatorios', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ControlesPostoperatorios', N'Fecha') IS NULL ALTER TABLE dbo.ControlesPostoperatorios ADD Fecha datetime NOT NULL CONSTRAINT DF_ControlesPostoperatorios_Fecha_Mig DEFAULT(getdate()) WITH VALUES;
    IF COL_LENGTH(N'dbo.ControlesPostoperatorios', N'CirugiaId') IS NULL ALTER TABLE dbo.ControlesPostoperatorios ADD CirugiaId int NULL;
    IF COL_LENGTH(N'dbo.ControlesPostoperatorios', N'DiasPostoperatorio') IS NULL ALTER TABLE dbo.ControlesPostoperatorios ADD DiasPostoperatorio int NULL;
    IF COL_LENGTH(N'dbo.ControlesPostoperatorios', N'Evolucion') IS NULL ALTER TABLE dbo.ControlesPostoperatorios ADD Evolucion nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.ControlesPostoperatorios', N'Dolor') IS NULL ALTER TABLE dbo.ControlesPostoperatorios ADD Dolor nvarchar(250) NULL;
    IF COL_LENGTH(N'dbo.ControlesPostoperatorios', N'Inflamacion') IS NULL ALTER TABLE dbo.ControlesPostoperatorios ADD Inflamacion nvarchar(250) NULL;
    IF COL_LENGTH(N'dbo.ControlesPostoperatorios', N'Observaciones') IS NULL ALTER TABLE dbo.ControlesPostoperatorios ADD Observaciones nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.ControlesPostoperatorios', N'ProximoControl') IS NULL ALTER TABLE dbo.ControlesPostoperatorios ADD ProximoControl datetime NULL;
END;
GO

/* Las FK se agregan después de que todas las tablas necesarias existen. */
IF OBJECT_ID(N'dbo.DocumentosClinicos', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Pacientes', N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_DocumentosClinicos_Pacientes')
    ALTER TABLE dbo.DocumentosClinicos WITH CHECK ADD CONSTRAINT FK_DocumentosClinicos_Pacientes FOREIGN KEY(PacienteId) REFERENCES dbo.Pacientes(PacienteId);
GO
IF OBJECT_ID(N'dbo.DocumentosClinicos', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Cirugias', N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_DocumentosClinicos_Cirugias')
    ALTER TABLE dbo.DocumentosClinicos WITH CHECK ADD CONSTRAINT FK_DocumentosClinicos_Cirugias FOREIGN KEY(CirugiaId) REFERENCES dbo.Cirugias(CirugiaId);
GO
IF OBJECT_ID(N'dbo.DocumentosClinicos', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Consultas', N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_DocumentosClinicos_Consultas')
    ALTER TABLE dbo.DocumentosClinicos WITH CHECK ADD CONSTRAINT FK_DocumentosClinicos_Consultas FOREIGN KEY(ConsultaId) REFERENCES dbo.Consultas(ConsultaId);
GO
IF OBJECT_ID(N'dbo.DocumentosClinicos', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_DocumentosClinicos_Usuarios')
    ALTER TABLE dbo.DocumentosClinicos WITH CHECK ADD CONSTRAINT FK_DocumentosClinicos_Usuarios FOREIGN KEY(UsuarioId) REFERENCES dbo.Usuarios(UsuarioId);
GO
IF OBJECT_ID(N'dbo.AuditoriaExpediente', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Pacientes', N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AuditoriaExpediente_Pacientes')
    ALTER TABLE dbo.AuditoriaExpediente WITH CHECK ADD CONSTRAINT FK_AuditoriaExpediente_Pacientes FOREIGN KEY(PacienteId) REFERENCES dbo.Pacientes(PacienteId);
GO
IF OBJECT_ID(N'dbo.AuditoriaExpediente', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AuditoriaExpediente_Usuarios')
    ALTER TABLE dbo.AuditoriaExpediente WITH CHECK ADD CONSTRAINT FK_AuditoriaExpediente_Usuarios FOREIGN KEY(UsuarioId) REFERENCES dbo.Usuarios(UsuarioId);
GO
IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Citas', N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Notificaciones_Citas')
    ALTER TABLE dbo.Notificaciones WITH CHECK ADD CONSTRAINT FK_Notificaciones_Citas FOREIGN KEY(CitaId) REFERENCES dbo.Citas(CitaId) ON DELETE SET NULL;
GO
IF OBJECT_ID(N'dbo.Noticias', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Noticias_Usuarios')
    ALTER TABLE dbo.Noticias WITH CHECK ADD CONSTRAINT FK_Noticias_Usuarios FOREIGN KEY(CreadaPorUsuarioId) REFERENCES dbo.Usuarios(UsuarioId) ON DELETE SET NULL;
GO
IF OBJECT_ID(N'dbo.Preoperatorios', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Cirugias', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Preoperatorios', N'CirugiaId') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Preoperatorios_Cirugias')
    ALTER TABLE dbo.Preoperatorios WITH CHECK ADD CONSTRAINT FK_Preoperatorios_Cirugias FOREIGN KEY(CirugiaId) REFERENCES dbo.Cirugias(CirugiaId);
GO
IF OBJECT_ID(N'dbo.ControlesPostoperatorios', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Cirugias', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ControlesPostoperatorios', N'CirugiaId') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_ControlesPostoperatorios_Cirugias')
    ALTER TABLE dbo.ControlesPostoperatorios WITH CHECK ADD CONSTRAINT FK_ControlesPostoperatorios_Cirugias FOREIGN KEY(CirugiaId) REFERENCES dbo.Cirugias(CirugiaId);
GO
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Pacientes', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Usuarios', N'PacienteId') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Usuarios_Pacientes')
    ALTER TABLE dbo.Usuarios WITH CHECK ADD CONSTRAINT FK_Usuarios_Pacientes FOREIGN KEY(PacienteId) REFERENCES dbo.Pacientes(PacienteId);
GO
IF OBJECT_ID(N'dbo.Pagos', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Pacientes', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Pagos', N'PacienteId') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Pagos_Pacientes')
    ALTER TABLE dbo.Pagos WITH CHECK ADD CONSTRAINT FK_Pagos_Pacientes FOREIGN KEY(PacienteId) REFERENCES dbo.Pacientes(PacienteId);
GO
IF OBJECT_ID(N'dbo.Pagos', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Seguros', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Pagos', N'SeguroId') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Pagos_Seguros')
    ALTER TABLE dbo.Pagos WITH CHECK ADD CONSTRAINT FK_Pagos_Seguros FOREIGN KEY(SeguroId) REFERENCES dbo.Seguros(SeguroId);
GO

/* Si se encontró una tabla parcial, rellena referencias nuevas antes de hacerlas obligatorias. */
IF OBJECT_ID(N'dbo.DocumentosClinicos', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.DocumentosClinicos', N'UsuarioId') IS NOT NULL
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.DocumentosClinicos WHERE PacienteId IS NULL)
        THROW 51004, 'DocumentosClinicos tiene PacienteId NULL; asigne cada documento a su paciente real antes de sincronizar.', 1;
    IF EXISTS(SELECT 1 FROM dbo.DocumentosClinicos WHERE UsuarioId IS NULL)
        THROW 51001, 'DocumentosClinicos tiene UsuarioId NULL; asigne el usuario responsable real antes de sincronizar.', 1;
    ALTER TABLE dbo.DocumentosClinicos ALTER COLUMN PacienteId int NOT NULL;
    ALTER TABLE dbo.DocumentosClinicos ALTER COLUMN UsuarioId int NOT NULL;
END;
GO
IF OBJECT_ID(N'dbo.AuditoriaExpediente', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.AuditoriaExpediente', N'PacienteId') IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.AuditoriaExpediente WHERE PacienteId IS NULL)
        THROW 51002, 'AuditoriaExpediente tiene PacienteId NULL; asigne cada registro al paciente correcto antes de sincronizar.', 1;
    IF COL_LENGTH(N'dbo.AuditoriaExpediente', N'UsuarioId') IS NOT NULL AND EXISTS(SELECT 1 FROM dbo.AuditoriaExpediente WHERE UsuarioId IS NULL)
        THROW 51003, 'AuditoriaExpediente tiene UsuarioId NULL; asigne cada registro al usuario correcto antes de sincronizar.', 1;
    ALTER TABLE dbo.AuditoriaExpediente ALTER COLUMN PacienteId int NOT NULL;
    ALTER TABLE dbo.AuditoriaExpediente ALTER COLUMN UsuarioId int NOT NULL;
END;
GO
IF OBJECT_ID(N'dbo.Preoperatorios', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Preoperatorios', N'CirugiaId') IS NOT NULL
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.Preoperatorios WHERE CirugiaId IS NULL)
        THROW 51005, 'Preoperatorios tiene CirugiaId NULL; vincule cada evaluación con su cirugía antes de sincronizar.', 1;
    ALTER TABLE dbo.Preoperatorios ALTER COLUMN CirugiaId int NOT NULL;
END;
GO
IF OBJECT_ID(N'dbo.ControlesPostoperatorios', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ControlesPostoperatorios', N'CirugiaId') IS NOT NULL
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.ControlesPostoperatorios WHERE CirugiaId IS NULL)
        THROW 51006, 'ControlesPostoperatorios tiene CirugiaId NULL; vincule cada control con su cirugía antes de sincronizar.', 1;
    ALTER TABLE dbo.ControlesPostoperatorios ALTER COLUMN CirugiaId int NOT NULL;
END;
GO

/* Índices usados por consultas del expediente y avisos. */
IF OBJECT_ID(N'dbo.DocumentosClinicos', N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.DocumentosClinicos') AND name=N'IX_DocumentosClinicos_Paciente_Fecha')
    CREATE INDEX IX_DocumentosClinicos_Paciente_Fecha ON dbo.DocumentosClinicos(PacienteId, FechaCarga DESC);
GO
IF OBJECT_ID(N'dbo.AuditoriaExpediente', N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AuditoriaExpediente') AND name=N'IX_AuditoriaExpediente_Paciente_Fecha')
    CREATE INDEX IX_AuditoriaExpediente_Paciente_Fecha ON dbo.AuditoriaExpediente(PacienteId, Fecha DESC);
GO
IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Notificaciones') AND name=N'IX_Notificaciones_Leida_FechaCreacion')
    CREATE INDEX IX_Notificaciones_Leida_FechaCreacion ON dbo.Notificaciones(Leida, FechaCreacion DESC);
GO
IF OBJECT_ID(N'dbo.Noticias', N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Noticias') AND name=N'IX_Noticias_Publicada_FechaPublicacion')
    CREATE INDEX IX_Noticias_Publicada_FechaPublicacion ON dbo.Noticias(Publicada, FechaPublicacion DESC);
GO
IF OBJECT_ID(N'dbo.ControlesPostoperatorios', N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ControlesPostoperatorios') AND name=N'IX_Controles_Cirugia_Fecha')
    CREATE INDEX IX_Controles_Cirugia_Fecha ON dbo.ControlesPostoperatorios(CirugiaId, Fecha);
GO
IF OBJECT_ID(N'dbo.Pagos', N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Pagos') AND name=N'IX_Pagos_PacienteId')
    CREATE INDEX IX_Pagos_PacienteId ON dbo.Pagos(PacienteId);
GO
