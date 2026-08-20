namespace CampoMarket.Web.Models;

public sealed class CatálogoViewModel
{
    public IEnumerable<Producto> Productos { get; set; } = [];
    public IEnumerable<Categoria> Categorias { get; set; } = [];
    public string? Categoria { get; set; }
    public string? Buscar { get; set; }
    public string? Orden { get; set; }
    public IReadOnlyList<OpcionSeleccion> OpcionesOrden { get; set; } = [];
}
