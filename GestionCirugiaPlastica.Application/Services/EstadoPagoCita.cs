namespace GestionCirugiaPlastica.Application.Services;

/// <summary>Calcula el estado de la cuenta sin convertir saldos pendientes en transacciones.</summary>
public static class EstadoPagoCita
{
    public static string Calcular(decimal costo, decimal recibido)
    {
        if (costo <= 0 || recibido >= costo)
            return "Pagada";

        return recibido > 0 ? "Parcial" : "Pendiente";
    }
}
