namespace CampoMarket.Web.Models;

public sealed class HistorialEstado
{
    public string Estado { get; set; } = "";
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}
