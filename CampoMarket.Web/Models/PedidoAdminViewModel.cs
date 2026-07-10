namespace CampoMarket.Web.Models;

public sealed class PedidoAdminViewModel
{
    public IEnumerable<Pedido> Pedidos { get; set; } = [];
    public string? Estado { get; set; }
    public string? Tipo { get; set; }
    public string? Buscar { get; set; }
    public bool Historial { get; set; }
    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
}
