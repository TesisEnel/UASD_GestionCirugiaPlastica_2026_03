/* Ejecutar una vez contra la base de datos de GestionCirugiaPlastica. */

IF COL_LENGTH(N'dbo.Citas', N'CodigoPublico') IS NULL
    ALTER TABLE dbo.Citas ADD CodigoPublico nvarchar(32) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Citas_CodigoPublico' AND object_id = OBJECT_ID(N'dbo.Citas'))
    CREATE UNIQUE INDEX UX_Citas_CodigoPublico ON dbo.Citas(CodigoPublico) WHERE CodigoPublico IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notificaciones
    (
        NotificacionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notificaciones PRIMARY KEY,
        Tipo nvarchar(50) NOT NULL,
        Titulo nvarchar(200) NOT NULL,
        Mensaje nvarchar(1000) NOT NULL,
        CitaId int NULL,
        FechaCreacion datetime2 NOT NULL CONSTRAINT DF_Notificaciones_FechaCreacion DEFAULT (GETDATE()),
        Leida bit NOT NULL CONSTRAINT DF_Notificaciones_Leida DEFAULT (0),
        CONSTRAINT FK_Notificaciones_Citas FOREIGN KEY (CitaId) REFERENCES dbo.Citas(CitaId) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notificaciones_Leida_FechaCreacion' AND object_id = OBJECT_ID(N'dbo.Notificaciones'))
    CREATE INDEX IX_Notificaciones_Leida_FechaCreacion ON dbo.Notificaciones(Leida, FechaCreacion DESC);
GO

IF OBJECT_ID(N'dbo.Noticias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Noticias
    (
        NoticiaId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Noticias PRIMARY KEY,
        Titulo nvarchar(200) NOT NULL,
        Resumen nvarchar(500) NULL,
        Contenido nvarchar(max) NOT NULL,
        FechaPublicacion datetime2 NOT NULL CONSTRAINT DF_Noticias_FechaPublicacion DEFAULT (GETDATE()),
        Publicada bit NOT NULL CONSTRAINT DF_Noticias_Publicada DEFAULT (0),
        CreadaPorUsuarioId int NULL,
        CONSTRAINT FK_Noticias_Usuarios FOREIGN KEY (CreadaPorUsuarioId) REFERENCES dbo.Usuarios(UsuarioId) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Noticias_Publicada_FechaPublicacion' AND object_id = OBJECT_ID(N'dbo.Noticias'))
    CREATE INDEX IX_Noticias_Publicada_FechaPublicacion ON dbo.Noticias(Publicada, FechaPublicacion DESC);
GO
