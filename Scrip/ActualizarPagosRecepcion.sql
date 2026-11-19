USE [GestionCirugiaPlastica];
GO

-- Extiende dbo.Pagos para cobros de recepción con paciente, cobertura y estado.
IF COL_LENGTH(N'dbo.Pagos', N'PacienteId') IS NULL
    ALTER TABLE dbo.Pagos ADD PacienteId int NULL;
GO

IF COL_LENGTH(N'dbo.Pagos', N'SeguroId') IS NULL
    ALTER TABLE dbo.Pagos ADD SeguroId int NULL;
GO

IF COL_LENGTH(N'dbo.Pagos', N'MontoSeguro') IS NULL
    ALTER TABLE dbo.Pagos ADD MontoSeguro decimal(12, 2) NOT NULL
        CONSTRAINT DF_Pagos_MontoSeguro DEFAULT (0) WITH VALUES;
GO

IF COL_LENGTH(N'dbo.Pagos', N'Estado') IS NULL
    ALTER TABLE dbo.Pagos ADD Estado nvarchar(250) NOT NULL
        CONSTRAINT DF_Pagos_Estado DEFAULT (N'Pagado') WITH VALUES;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Pagos_Pacientes')
    ALTER TABLE dbo.Pagos WITH CHECK ADD CONSTRAINT FK_Pagos_Pacientes
        FOREIGN KEY (PacienteId) REFERENCES dbo.Pacientes (PacienteId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Pagos_Seguros')
    ALTER TABLE dbo.Pagos WITH CHECK ADD CONSTRAINT FK_Pagos_Seguros
        FOREIGN KEY (SeguroId) REFERENCES dbo.Seguros (SeguroId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Pagos_MontoSeguro')
    ALTER TABLE dbo.Pagos WITH CHECK ADD CONSTRAINT CK_Pagos_MontoSeguro
        CHECK (MontoSeguro >= 0 AND MontoSeguro <= Monto);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Pagos_Estado')
    ALTER TABLE dbo.Pagos WITH CHECK ADD CONSTRAINT CK_Pagos_Estado
        CHECK (Estado IN (N'Pagado', N'Pendiente'));
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pagos_PacienteId' AND object_id = OBJECT_ID(N'dbo.Pagos'))
    CREATE INDEX IX_Pagos_PacienteId ON dbo.Pagos (PacienteId);
GO
