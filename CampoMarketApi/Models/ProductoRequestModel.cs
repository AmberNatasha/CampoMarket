namespace CampoMarketApi.Models;

public sealed class ProductoRequestModel
{
    public int IdProducto { get; set; }
    public int IdCategoria { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; } = true;
}