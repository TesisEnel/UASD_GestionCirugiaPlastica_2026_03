/*
  Datos demo idempotentes para probar el expediente clínico.
  Ejecutar en GestionCirugiaPlastica después de iniciar el backend al menos una vez.
  La imagen incluida se encuentra en:
  GestionCirugiaPlastica.Api/App_Data/clinical-images/demo-cirugia.png
  RutaArchivo guarda una ruta relativa; el archivo se sirve por GET autenticado
  api/Fotografias/archivo/{id}, no desde wwwroot público.
*/
SET NOCOUNT ON;

DECLARE @PacienteId int;
DECLARE @ProcedimientoId int;
DECLARE @CirugiaId int;

SELECT TOP (1) @PacienteId = PacienteId
FROM dbo.Pacientes
ORDER BY PacienteId;

IF @PacienteId IS NULL
    THROW 51000, 'No hay pacientes en dbo.Pacientes para asociar el ejemplo.', 1;

SELECT @ProcedimientoId = ProcedimientoId
FROM dbo.Procedimientos
WHERE Nombre = N'Procedimiento demostrativo de expediente';

IF @ProcedimientoId IS NULL
BEGIN
    INSERT dbo.Procedimientos (FechaRegistro, Nombre, Descripcion, PrecioBase)
    VALUES (GETDATE(), N'Procedimiento demostrativo de expediente',
            N'Registro de ejemplo para verificar el historial clínico y la galería.', 0);
    SET @ProcedimientoId = CONVERT(int, SCOPE_IDENTITY());
END;

SELECT @CirugiaId = CirugiaId
FROM dbo.Cirugias
WHERE PacienteId = @PacienteId
  AND ProcedimientoId = @ProcedimientoId
  AND DiagnosticoPreoperatorio = N'Diagnóstico de demostración: ejemplo para validar la visualización del expediente.';

IF @CirugiaId IS NULL
BEGIN
    INSERT dbo.Cirugias
        (FechaCirugia, PacienteId, ProcedimientoId, DiagnosticoPreoperatorio, Observaciones, Estado)
    VALUES
        (DATEADD(day, -30, GETDATE()), @PacienteId, @ProcedimientoId,
         N'Diagnóstico de demostración: ejemplo para validar la visualización del expediente.',
         N'Registro sintético de prueba; no representa una atención clínica real.', N'Completada');
    SET @CirugiaId = CONVERT(int, SCOPE_IDENTITY());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Fotografias WHERE CirugiaId = @CirugiaId AND RutaArchivo = N'uploads/cirugias/demo-cirugia.png')
BEGIN
    INSERT dbo.Fotografias (Fecha, CirugiaId, Tipo, RutaArchivo, Descripcion)
    VALUES (GETDATE(), @CirugiaId, N'image/png', N'uploads/cirugias/demo-cirugia.png',
            N'Imagen sintética de demostración para validar la galería.');
END;

SELECT @PacienteId AS PacienteId, @ProcedimientoId AS ProcedimientoId, @CirugiaId AS CirugiaId;
