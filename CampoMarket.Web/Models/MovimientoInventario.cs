namespace CampoMarket.Web.Models;

public sealed class MovimientoInventario
{
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public int Cantidad { get; set; }
    public string Motivo { get; set; } = "";
}
