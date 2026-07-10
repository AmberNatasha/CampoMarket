namespace CampoMarket.Web.Models;

public sealed class AuditLogItem
{
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string Correo { get; set; } = "";
    public string Evento { get; set; } = "";
    public string Ip { get; set; } = "";
}
