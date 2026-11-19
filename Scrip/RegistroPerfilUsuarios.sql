-- Ejecutar una sola vez en la base de datos de la clínica.
-- Agrega la URL de la foto y permite múltiples fichas sin cédula.
IF COL_LENGTH(N'dbo.Usuarios', N'FotoUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios ADD FotoUrl nvarchar(500) NULL;
END;
GO

-- La cédula sigue siendo única cuando se proporciona, pero NULL no debe impedir
-- que varias personas creen su ficha con teléfono o solo con nombre de usuario.
DECLARE @indexName sysname;
DECLARE @isConstraint bit;
DECLARE @dropSql nvarchar(max);

WHILE EXISTS
(
    SELECT 1
    FROM sys.indexes i
    JOIN sys.index_columns ic
      ON ic.object_id = i.object_id
     AND ic.index_id = i.index_id
     AND ic.key_ordinal = 1
    JOIN sys.columns c
      ON c.object_id = ic.object_id
     AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.Pacientes')
      AND i.is_unique = 1
      AND i.is_primary_key = 0
      AND i.has_filter = 0
      AND c.name = N'Cedula'
      AND NOT EXISTS
      (
          SELECT 1 FROM sys.index_columns extra
          WHERE extra.object_id = i.object_id
            AND extra.index_id = i.index_id
            AND extra.key_ordinal > 1
      )
)
BEGIN
    SELECT TOP (1)
        @indexName = i.name,
        @isConstraint = i.is_unique_constraint
    FROM sys.indexes i
    JOIN sys.index_columns ic
      ON ic.object_id = i.object_id
     AND ic.index_id = i.index_id
     AND ic.key_ordinal = 1
    JOIN sys.columns c
      ON c.object_id = ic.object_id
     AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.Pacientes')
      AND i.is_unique = 1
      AND i.is_primary_key = 0
      AND i.has_filter = 0
      AND c.name = N'Cedula'
      AND NOT EXISTS
      (
          SELECT 1 FROM sys.index_columns extra
          WHERE extra.object_id = i.object_id
            AND extra.index_id = i.index_id
            AND extra.key_ordinal > 1
      );

    IF @isConstraint = 1
        SET @dropSql = N'ALTER TABLE dbo.Pacientes DROP CONSTRAINT ' + QUOTENAME(@indexName) + N';';
    ELSE
        SET @dropSql = N'DROP INDEX ' + QUOTENAME(@indexName) + N' ON dbo.Pacientes;';

    EXEC sys.sp_executesql @dropSql;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Pacientes')
      AND name = N'UX_Pacientes_Cedula_NotNull'
)
BEGIN
    CREATE UNIQUE INDEX UX_Pacientes_Cedula_NotNull
        ON dbo.Pacientes(Cedula)
        WHERE Cedula IS NOT NULL;
END;
GO
