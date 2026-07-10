namespace CampoMarket.Web.Models;

public sealed class CarritoLineaViewModel
{
    public Producto Producto { get; set; } = new();
    public int Cantidad { get; set; }
    public decimal Subtotal => Producto.Precio * Cantidad;
}
