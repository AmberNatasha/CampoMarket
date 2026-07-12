namespace CampoMarket.Web.Models;

public sealed class CatálogoViewModel
{
    public IEnumerable<Producto> Productos { get; set; } = [];
    public IEnumerable<Categoria> Categorias { get; set; } = [];
    public string? Categoria { get; set; }
    public string? Buscar { get; set; }
    public string? Orden { get; set; }
    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
}
