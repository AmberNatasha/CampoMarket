namespace CampoMarket.Web.Models;

public sealed class Pedido
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public int UsuarioId { get; set; }
    public string Estado { get; set; } = EstadosPedido.Pendiente;
    public string TipoEntrega { get; set; } = TiposEntrega.Express;
    public string DireccionEntrega { get; set; } = "";
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CanceladoUtc { get; set; }
    public decimal Total { get; set; }
    public List<PedidoDetalle> Detalles { get; set; } = [];
    public List<HistorialEstado> Historial { get; set; } = [];
}
