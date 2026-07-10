namespace CampoMarket.Web.Models;

public sealed class PedidoDetalle
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => PrecioUnitario * Cantidad;
}
