using System.ComponentModel.DataAnnotations;

namespace CampoMarket.Web.Models;

public static class RolesCampo
{
    public const string Cliente = "Cliente";
    public const string Admin = "Admin";
}

public sealed class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Rol { get; set; } = RolesCampo.Cliente;
    public string PasswordHash { get; set; } = "";
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHastaUtc { get; set; }
}

public sealed class DireccionCliente
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Alias { get; set; } = "";
    public string Provincia { get; set; } = "";
    public string Canton { get; set; } = "";
    public string Distrito { get; set; } = "";
    public string SenasExactas { get; set; } = "";
    public string Detalle { get; set; } = "";
    public bool Predeterminada { get; set; }
}

public sealed class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public bool Activa { get; set; } = true;
}

public sealed class Producto
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; } = 5;
    public string ImagenUrl { get; set; } = "";
    public bool Activo { get; set; } = true;
    public DateTime ActualizadoUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CarritoItem
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}

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

public sealed class PedidoDetalle
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => PrecioUnitario * Cantidad;
}

public sealed class HistorialEstado
{
    public string Estado { get; set; } = "";
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}

public sealed class MovimientoInventario
{
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public int Cantidad { get; set; }
    public string Motivo { get; set; } = "";
}

public sealed class AuditLogItem
{
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string Correo { get; set; } = "";
    public string Evento { get; set; } = "";
    public string Ip { get; set; } = "";
}

public sealed class LogErrorItem
{
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string Ruta { get; set; } = "";
    public string Mensaje { get; set; } = "";
}

public sealed class PasswordResetToken
{
    public int UsuarioId { get; set; }
    public string Token { get; set; } = "";
    public DateTime ExpiraUtc { get; set; }
    public bool Usado { get; set; }
}

public static class EstadosPedido
{
    public const string Pendiente = "Pendiente";
    public const string Preparando = "Preparando";
    public const string Listo = "Listo";
    public const string Entregado = "Entregado";
    public const string Cancelado = "Cancelado";
}

public static class TiposEntrega
{
    public const string Express = "Express";
    public const string Recoleccion = "Recoleccion";
}

public sealed class CatalogoViewModel
{
    public IEnumerable<Producto> Productos { get; set; } = [];
    public IEnumerable<Categoria> Categorias { get; set; } = [];
    public string? Categoria { get; set; }
    public string? Buscar { get; set; }
    public string? Orden { get; set; }
    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
}

public sealed class CarritoViewModel
{
    public IReadOnlyList<CarritoLineaViewModel> Lineas { get; set; } = [];
    public IEnumerable<DireccionCliente> Direcciones { get; set; } = [];
    public decimal Total => Lineas.Sum(x => x.Subtotal);
}

public sealed class CarritoLineaViewModel
{
    public Producto Producto { get; set; } = new();
    public int Cantidad { get; set; }
    public decimal Subtotal => Producto.Precio * Cantidad;
}

public sealed class RegistroViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo valido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = "";

    [Phone(ErrorMessage = "Ingresa un telefono valido.")]
    public string Telefono { get; set; } = "";

    public string Direccion { get; set; } = "";
}

public sealed class PerfilViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = "";

    [Phone(ErrorMessage = "Ingresa un telefono valido.")]
    [RegularExpression(@"^[0-9+\-\s]{7,20}$", ErrorMessage = "El telefono solo puede usar digitos, espacios, guiones o prefijo.")]
    public string Telefono { get; set; } = "";

    public string Direccion { get; set; } = "";
}

public sealed class CambiarPasswordViewModel
{
    [Required(ErrorMessage = "Ingresa tu contrasena actual.")]
    public string PasswordActual { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la nueva contrasena.")]
    [MinLength(8, ErrorMessage = "La nueva contrasena debe tener al menos 8 caracteres.")]
    public string PasswordNuevo { get; set; } = "";
}

public sealed class DireccionFormViewModel
{
    public int Id { get; set; }

    [Required] public string Alias { get; set; } = "";
    [Required] public string Provincia { get; set; } = "";
    [Required] public string Canton { get; set; } = "";
    [Required] public string Distrito { get; set; } = "";
    [Required] public string SenasExactas { get; set; } = "";
    public bool Predeterminada { get; set; }
}

public sealed class RecuperarPasswordViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo valido.")]
    public string Correo { get; set; } = "";
}

public sealed class RestablecerPasswordViewModel
{
    [Required]
    public string Token { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la nueva contrasena.")]
    [MinLength(8, ErrorMessage = "La nueva contrasena debe tener al menos 8 caracteres.")]
    public string PasswordNuevo { get; set; } = "";
}

public sealed class CategoriaFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = "";

    public string Descripcion { get; set; } = "";
}

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

public sealed class ProductoVendidoViewModel
{
    public string Producto { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
}

public sealed class ProductoFormViewModel
{
    public int Id { get; set; }
    [Required] public string Nombre { get; set; } = "";
    [Required] public string Descripcion { get; set; } = "";
    [Range(0.01, 100000)] public decimal Precio { get; set; }
    [Range(0, 100000)] public int Stock { get; set; }
    [Range(0, 100000)] public int StockMinimo { get; set; }
    [Required] public int CategoriaId { get; set; }
    public string ImagenUrl { get; set; } = "";
}
