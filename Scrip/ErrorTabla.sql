USE [GestionCirugiaPlastica];
GO

IF COL_LENGTH(N'dbo.HistorialClinico', N'AntecedentesPersonales') IS NULL
BEGIN
    ALTER TABLE dbo.HistorialClinico ADD AntecedentesPersonales nvarchar(max) NULL;
END;
GO

IF COL_LENGTH(N'dbo.HistorialClinico', N'AntecedentesFamiliares') IS NULL
BEGIN
    ALTER TABLE dbo.HistorialClinico ADD AntecedentesFamiliares nvarchar(max) NULL;
END;
GO