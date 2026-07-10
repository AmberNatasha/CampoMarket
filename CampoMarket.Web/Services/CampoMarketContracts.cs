using CampoMarket.Web.Models;

namespace CampoMarket.Web.Services;

public interface IUserService
{
    IReadOnlyList<Usuario> Clientes { get; }
    Usuario? FindUser(int id);
    (bool Ok, string Message, Usuario? User) Register(string nombre, string correo, string password, string telefono, string direccion);
    (bool Ok, string Message, Usuario? User) Login(string correo, string password, string ip = "");
    (bool Ok, string Message) UpdateProfile(int userId, string nombre, string telefono, string direccion);
    (bool Ok, string Message) ChangePassword(int userId, string actual, string nuevo);
}

public interface IPasswordResetService
{
    (bool Ok, string Message, string? Token) RequestPasswordReset(string correo);
    (bool Ok, string Message) ResetPassword(string token, string nuevo);
}

public interface ICatalogService
{
    IReadOnlyList<Categoria> Categorias { get; }
    IReadOnlyList<Producto> Productos { get; }
    IEnumerable<Producto> BuscarProductos(string? categoria, string? buscar, string? orden);
    Producto? FindProduct(int id);
    (bool Ok, string Message) SaveProduct(ProductoFormViewModel form);
    (bool Ok, string Message) DeactivateProduct(int id);
    (bool Ok, string Message) AdjustStock(int id, int cantidad, string motivo);
    (bool Ok, string Message) SaveCategory(CategoriaFormViewModel form);
    (bool Ok, string Message) DeleteCategory(int id);
}

public interface ICartService
{
    IReadOnlyList<CarritoLineaViewModel> GetCart(int userId);
    (bool Ok, string Message) AddToCart(int userId, int productId, int cantidad);
    void UpdateCart(int userId, int productId, int cantidad);
    void RemoveFromCart(int userId, int productId);
    void ClearCart(int userId);
}

public interface IAddressService
{
    IEnumerable<DireccionCliente> GetAddresses(int userId);
    DireccionCliente? FindAddress(int userId, int id);
    (bool Ok, string Message) SaveAddress(int userId, DireccionFormViewModel form);
    (bool Ok, string Message) DeleteAddress(int userId, int id);
}

public interface IOrderService
{
    IReadOnlyList<Pedido> Pedidos { get; }
    (bool Ok, string Message, Pedido? Pedido) CreateOrder(int userId, string tipoEntrega, string direccionEntrega);
    (bool Ok, string Message) CancelOrder(int userId, int orderId);
    (bool Ok, string Message) AdvanceOrder(int orderId);
    IEnumerable<Pedido> PedidosCliente(int userId);
    Pedido? FindOrder(int id);
    IEnumerable<Pedido> BuscarPedidosAdmin(string? estado, string? tipo, string? buscar, bool incluirCerrados = false);
    Usuario? UsuarioPedido(int pedidoUsuarioId);
}

public interface IReportService
{
    IEnumerable<ProductoVendidoViewModel> ProductosMasVendidos(DateTime? desde, DateTime? hasta, int? categoriaId);
    IEnumerable<MovimientoInventario> FiltrarMovimientos(DateTime? desde, DateTime? hasta, int? productoId);
}

public interface IAuditService
{
    IReadOnlyList<AuditLogItem> AuditLogs { get; }
    IReadOnlyList<LogErrorItem> ErrorLogs { get; }
    void LogError(string ruta, string mensaje);
}

public interface IProductImageService
{
    Task<(bool Ok, string Message, string? Url)> SaveAsync(IFormFile image);
}

public interface IAuthSessionService
{
    Task SignInAsync(HttpContext httpContext, Usuario user);
    Task SignOutAsync(HttpContext httpContext);
}
