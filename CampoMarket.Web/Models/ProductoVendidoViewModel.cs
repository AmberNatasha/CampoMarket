namespace CampoMarket.Web.Models;

public sealed class ProductoVendidoViewModel
{
    public string Producto { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
}
