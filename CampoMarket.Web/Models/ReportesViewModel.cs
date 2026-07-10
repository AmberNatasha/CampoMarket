namespace CampoMarket.Web.Models;

public sealed class ReportesViewModel
{
    public IEnumerable<ProductoVendidoViewModel> MasVendidos { get; set; } = [];
    public IEnumerable<MovimientoInventario> Movimientos { get; set; } = [];
    public IEnumerable<Categoria> Categorias { get; set; } = [];
    public IEnumerable<Producto> Productos { get; set; } = [];
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? CategoriaId { get; set; }
    public int? ProductoId { get; set; }
}
