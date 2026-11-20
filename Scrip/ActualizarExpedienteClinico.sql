/* Actualización aditiva para el expediente digital. Ejecutar en la base TangerClinic. */
IF COL_LENGTH('dbo.HistorialClinico', 'AntecedentesPersonales') IS NULL
    ALTER TABLE dbo.HistorialClinico ADD AntecedentesPersonales NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.HistorialClinico', 'AntecedentesFamiliares') IS NULL
    ALTER TABLE dbo.HistorialClinico ADD AntecedentesFamiliares NVARCHAR(MAX) NULL;
GO

IF OBJECT_ID('dbo.DocumentosClinicos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentosClinicos
    (
        DocumentoClinicoId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentosClinicos PRIMARY KEY,
        PacienteId INT NOT NULL,
        CirugiaId INT NULL,
        ConsultaId INT NULL,
        Categoria NVARCHAR(40) NOT NULL,
        NombreArchivo NVARCHAR(250) NOT NULL,
        ContentType NVARCHAR(120) NOT NULL,
        RutaArchivo NVARCHAR(500) NOT NULL,
        Descripcion NVARCHAR(500) NULL,
        FechaCarga DATETIME2 NOT NULL CONSTRAINT DF_DocumentosClinicos_FechaCarga DEFAULT SYSUTCDATETIME(),
        UsuarioId INT NOT NULL,
        CONSTRAINT FK_DocumentosClinicos_Pacientes FOREIGN KEY (PacienteId) REFERENCES dbo.Pacientes(PacienteId),
        CONSTRAINT FK_DocumentosClinicos_Cirugias FOREIGN KEY (CirugiaId) REFERENCES dbo.Cirugias(CirugiaId),
        CONSTRAINT FK_DocumentosClinicos_Consultas FOREIGN KEY (ConsultaId) REFERENCES dbo.Consultas(ConsultaId),
        CONSTRAINT FK_DocumentosClinicos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId)
    );
    CREATE INDEX IX_DocumentosClinicos_Paciente_Fecha ON dbo.DocumentosClinicos(PacienteId, FechaCarga DESC);
END;
GO

IF OBJECT_ID('dbo.AuditoriaExpediente', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaExpediente
    (
        AuditoriaExpedienteId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditoriaExpediente PRIMARY KEY,
        PacienteId INT NOT NULL,
        UsuarioId INT NOT NULL,
        Fecha DATETIME2 NOT NULL CONSTRAINT DF_AuditoriaExpediente_Fecha DEFAULT SYSUTCDATETIME(),
        Entidad NVARCHAR(80) NOT NULL,
        RegistroId INT NULL,
        Accion NVARCHAR(40) NOT NULL,
        Detalle NVARCHAR(500) NULL,
        CONSTRAINT FK_AuditoriaExpediente_Pacientes FOREIGN KEY (PacienteId) REFERENCES dbo.Pacientes(PacienteId),
        CONSTRAINT FK_AuditoriaExpediente_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(UsuarioId)
    );
    CREATE INDEX IX_AuditoriaExpediente_Paciente_Fecha ON dbo.AuditoriaExpediente(PacienteId, Fecha DESC);
END;
GO
