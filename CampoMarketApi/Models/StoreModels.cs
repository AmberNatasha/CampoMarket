namespace CampoMarketApi.Models;

public sealed class CategoriaDto { public int Id { get; set; } public string Nombre { get; set; } = ""; public string Descripcion { get; set; } = ""; public bool Activa { get; set; } }
public sealed class ProductoDto { public int Id { get; set; } public int CategoriaId { get; set; } public string Nombre { get; set; } = ""; public string Descripcion { get; set; } = ""; public decimal Precio { get; set; } public int Stock { get; set; } public int StockMinimo { get; set; } public string ImagenUrl { get; set; } = ""; public bool Activo { get; set; } public DateTime ActualizadoUtc { get; set; } }
public sealed class UsuarioDto { public int Id { get; set; } public string Nombre { get; set; } = ""; public string Correo { get; set; } = ""; public string Telefono { get; set; } = ""; public string Direccion { get; set; } = ""; public string Rol { get; set; } = ""; public string PasswordHash { get; set; } = ""; public int IntentosFallidos { get; set; } public DateTime? BloqueadoHastaUtc { get; set; } }
public sealed class DireccionDto { public int Id { get; set; } public int UsuarioId { get; set; } public string Alias { get; set; } = ""; public string Provincia { get; set; } = ""; public string Canton { get; set; } = ""; public string Distrito { get; set; } = ""; public string SenasExactas { get; set; } = ""; public string Detalle { get; set; } = ""; public bool Predeterminada { get; set; } }
public sealed class CarritoLineaDto { public ProductoDto Producto { get; set; } = new(); public int Cantidad { get; set; } }
public sealed class PedidoDto { public int Id { get; set; } public string Numero { get; set; } = ""; public int UsuarioId { get; set; } public string Estado { get; set; } = ""; public string TipoEntrega { get; set; } = ""; public string DireccionEntrega { get; set; } = ""; public DateTime FechaUtc { get; set; } public DateTime? CanceladoUtc { get; set; } public decimal Total { get; set; } public List<PedidoDetalleDto> Detalles { get; set; } = []; public List<HistorialDto> Historial { get; set; } = []; }
public sealed class PedidoDetalleDto { public int ProductoId { get; set; } public string ProductoNombre { get; set; } = ""; public int Cantidad { get; set; } public decimal PrecioUnitario { get; set; } }
public sealed class HistorialDto { public string Estado { get; set; } = ""; public DateTime FechaUtc { get; set; } }
public sealed class ProductoVendidoDto { public string Producto { get; set; } = ""; public int Cantidad { get; set; } public decimal Total { get; set; } }
public sealed class MovimientoDto { public DateTime FechaUtc { get; set; } public int ProductoId { get; set; } public string ProductoNombre { get; set; } = ""; public string Tipo { get; set; } = ""; public int Cantidad { get; set; } public string Motivo { get; set; } = ""; }
public sealed class AuditoriaDto { public DateTime FechaUtc { get; set; } public string Correo { get; set; } = ""; public string Evento { get; set; } = ""; public string Ip { get; set; } = ""; }
public sealed class ErrorDto { public DateTime FechaUtc { get; set; } public string Ruta { get; set; } = ""; public string Mensaje { get; set; } = ""; }

public sealed record CategoriaRequest(int Id, string Nombre, string Descripcion);
public sealed record DireccionRequest(int Id, string Alias, string Provincia, string Canton, string Distrito, string SenasExactas, bool Predeterminada);
public sealed record CarritoRequest(int ProductoId, int Cantidad);
public sealed record CrearPedidoRequest(string TipoEntrega, string DireccionEntrega);
public sealed record ErrorRequest(string Ruta, string Mensaje);
public sealed record RegistroRequest(string Nombre,string Correo,string Password,string Telefono,string Direccion);
public sealed record PerfilRequest(string Nombre,string Telefono,string Direccion);
public sealed record CambioPasswordRequest(string Actual,string Nuevo);
public sealed record CorreoRequest(string Correo);
public sealed record ValidarCodigoRequest(string Correo,string Codigo);
public sealed record RestablecerRequest(string Token,string Nuevo);
public sealed class ResetTokenDto { public int UsuarioId { get; set; } public string Token { get; set; } = ""; public DateTime ExpiraUtc { get; set; } public bool Usado { get; set; } }
