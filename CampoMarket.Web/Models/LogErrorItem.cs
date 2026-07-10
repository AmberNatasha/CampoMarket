namespace CampoMarket.Web.Models;

public sealed class LogErrorItem
{
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string Ruta { get; set; } = "";
    public string Mensaje { get; set; } = "";
}
