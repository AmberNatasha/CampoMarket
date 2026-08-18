using CampoMarket.Web.Models;

namespace CampoMarket.Web.Services;

public sealed class ApiCommerceService(ApiRequestClient api) : ICartService, IOrderService, IReportService
{
    public IReadOnlyList<Pedido> Pedidos => api.Get<List<Pedido>>("api/store/pedidos?incluirCerrados=true");
    public IReadOnlyList<CarritoLineaViewModel> GetCart(int userId) => api.Get<List<CarritoLineaViewModel>>("api/store/carrito");
    public (bool Ok, string Message) AddToCart(int userId,int productId,int cantidad)
    { var r=api.Post<ApiResult>("api/store/carrito",new { ProductoId=productId,Cantidad=cantidad }); return (r.Ok,r.Message); }
    public void UpdateCart(int userId,int productId,int cantidad) => api.Put<ApiResult>("api/store/carrito",new { ProductoId=productId,Cantidad=cantidad });
    public void RemoveFromCart(int userId,int productId) => api.Delete($"api/store/carrito/{productId}");
    public void ClearCart(int userId) => api.Delete("api/store/carrito");
    public (bool Ok,string Message,Pedido? Pedido) CreateOrder(int userId,string tipoEntrega,string direccionEntrega)
    { var order=api.Post<Pedido>("api/store/pedidos",new { tipoEntrega,direccionEntrega }); return (true,$"Pedido {order.Numero} generado.",order); }
    public (bool Ok,string Message) CancelOrder(int userId,int orderId)
    { var r=api.Post<ApiResult>($"api/store/pedidos/{orderId}/cancelar"); return (r.Ok,r.Message); }
    public (bool Ok,string Message) AdvanceOrder(int orderId)
    { var r=api.Post<ApiResult>($"api/store/pedidos/{orderId}/avanzar"); return (r.Ok,r.Message); }
    public IEnumerable<Pedido> PedidosCliente(int userId) => api.Get<List<Pedido>>("api/store/pedidos/mios");
    public Pedido? FindOrder(int id)
    { try { return api.Get<Pedido>($"api/store/pedidos/{id}"); } catch { return null; } }
    public IEnumerable<Pedido> BuscarPedidosAdmin(string? estado,string? tipo,string? buscar,bool incluirCerrados=false)
    {
        var query=$"?incluirCerrados={incluirCerrados.ToString().ToLowerInvariant()}&estado={E(estado)}&tipo={E(tipo)}&buscar={E(buscar)}";
        return api.Get<List<Pedido>>("api/store/pedidos"+query);
    }
    public Usuario? UsuarioPedido(int pedidoUsuarioId)
    { try { return api.Get<Usuario>($"api/store/usuarios/{pedidoUsuarioId}"); } catch { return null; } }
    public IEnumerable<ProductoVendidoViewModel> ProductosMasVendidos(DateTime? desde,DateTime? hasta,int? categoriaId) =>
        api.Get<List<ProductoVendidoViewModel>>($"api/store/reportes/productos?desde={D(desde)}&hasta={D(hasta)}&categoriaId={categoriaId}");
    public IEnumerable<MovimientoInventario> FiltrarMovimientos(DateTime? desde,DateTime? hasta,int? productoId) =>
        api.Get<List<MovimientoInventario>>($"api/store/reportes/movimientos?desde={D(desde)}&hasta={D(hasta)}&productoId={productoId}");
    private static string E(string? value)=>Uri.EscapeDataString(value??"");
    private static string D(DateTime? value)=>value?.ToString("yyyy-MM-dd")??"";
}

public class ApiResult { public bool Ok { get; set; } public string Message { get; set; } = ""; public string? Token { get; set; } }
