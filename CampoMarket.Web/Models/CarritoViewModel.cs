namespace CampoMarket.Web.Models;

public sealed class CarritoViewModel
{
    public IReadOnlyList<CarritoLineaViewModel> Lineas { get; set; } = [];
    public IEnumerable<DireccionCliente> Direcciones { get; set; } = [];
    public decimal Total => Lineas.Sum(x => x.Subtotal);
}
