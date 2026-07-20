namespace CampoMarketApi.Models;

public sealed class ProductoResponseModel
{
    public int Id_Producto { get; set; }
    public string Nombre_Producto { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string? Imagen_Url { get; set; }
    public int Id_Categoria { get; set; }
}