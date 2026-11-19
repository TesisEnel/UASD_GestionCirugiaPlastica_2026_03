-- Agrega la propiedad TieneSeguro a dbo.Pacientes.
-- Los registros existentes se inicializan en false.
IF COL_LENGTH(N'dbo.Pacientes', N'TieneSeguro') IS NULL
BEGIN
    ALTER TABLE dbo.Pacientes
    ADD TieneSeguro bit NOT NULL
        CONSTRAINT DF_Pacientes_TieneSeguro DEFAULT (0) WITH VALUES;
END;
GO
