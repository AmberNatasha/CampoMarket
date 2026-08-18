using System.Data;
using CampoMarketApi.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CampoMarketApi.Repositories;

public sealed class StoreRepository(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("CampoMarket")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:CampoMarket.");

    private SqlConnection Open() => new(_connectionString);

    public IReadOnlyList<CategoriaDto> Categorias()
    { using var db = Open(); return db.Query<CategoriaDto>("sp_Categoria_Listar", commandType: CommandType.StoredProcedure).ToList(); }

    public void GuardarCategoria(CategoriaRequest request)
    { using var db=Open(); db.Execute("sp_Categoria_Guardar", new { id_categoria=request.Id, nombre_categoria=request.Nombre, descripcion=request.Descripcion, activo=true }, commandType:CommandType.StoredProcedure); }

    public void EliminarCategoria(int id)
    { using var db=Open(); db.Execute("sp_Categoria_Desactivar", new { id_categoria=id }, commandType:CommandType.StoredProcedure); }

    public IReadOnlyList<ProductoDto> Productos()
    { using var db = Open(); return db.Query<ProductoDto>("sp_Producto_ListarStore", commandType: CommandType.StoredProcedure).ToList(); }

    public IReadOnlyList<UsuarioDto> Usuarios(int? id = null, string? correo = null, bool clientes = false)
    { using var db = Open(); return db.Query<UsuarioDto>("sp_Usuario_Obtener", new { id_usuario=id, correo, solo_clientes=clientes }, commandType: CommandType.StoredProcedure).ToList(); }

    public int CrearUsuario(RegistroRequest request,string passwordHash)
    { using var db=Open(); var p=new DynamicParameters(); p.Add("nombre",request.Nombre);p.Add("correo",request.Correo);p.Add("contrasena_hash",passwordHash);p.Add("telefono",request.Telefono);p.Add("rol","Cliente");p.Add("id_usuario",dbType:DbType.Int32,direction:ParameterDirection.Output);db.Execute("sp_Usuario_Registrar",p,commandType:CommandType.StoredProcedure);return p.Get<int>("id_usuario"); }
    public void ActualizarPerfil(int id,PerfilRequest request)
    { using var db=Open();db.Execute("sp_Usuario_ActualizarPerfil",new{id_usuario=id,nombre=request.Nombre,telefono=request.Telefono},commandType:CommandType.StoredProcedure); }
    public void ActualizarPassword(int id,string hash)
    { using var db=Open();db.Execute("sp_Usuario_ActualizarPassword",new{id_usuario=id,contrasena_hash=hash},commandType:CommandType.StoredProcedure); }
    public void CrearToken(int id,string hash,DateTime expires)
    { using var db=Open();db.Execute("sp_Token_Crear",new{id_usuario=id,token_hash=hash,fecha_expiracion=expires},commandType:CommandType.StoredProcedure); }
    public ResetTokenDto? ObtenerToken(string hash)
    { using var db=Open();return db.QueryFirstOrDefault<ResetTokenDto>("sp_Token_Obtener",new{token_hash=hash},commandType:CommandType.StoredProcedure); }
    public void UsarToken(string hash)
    { using var db=Open();db.Execute("sp_Token_MarcarUsado",new{token_hash=hash},commandType:CommandType.StoredProcedure); }

    public IReadOnlyList<DireccionDto> Direcciones(int userId)
    { using var db = Open(); return db.Query<DireccionDto>("sp_Direccion_Listar", new { id_usuario=userId }, commandType: CommandType.StoredProcedure).ToList(); }

    public void GuardarDireccion(int userId, DireccionRequest request)
    { using var db = Open(); db.Execute("sp_Direccion_Guardar", new { id_direccion=request.Id, id_usuario=userId, provincia=request.Provincia, canton=request.Canton, distrito=request.Distrito, senas_exactas=request.SenasExactas, predeterminada=request.Predeterminada }, commandType: CommandType.StoredProcedure); }

    public void EliminarDireccion(int userId, int id)
    { using var db = Open(); db.Execute("sp_Direccion_Eliminar", new { id_usuario=userId, id_direccion=id }, commandType: CommandType.StoredProcedure); }

    public IReadOnlyList<CarritoLineaDto> Carrito(int userId)
    { using var db = Open(); return db.Query<ProductCartRow>("sp_Carrito_Listar", new { id_usuario=userId }, commandType: CommandType.StoredProcedure).Select(x => new CarritoLineaDto { Producto=x.ToProduct(), Cantidad=x.Cantidad }).ToList(); }

    public void ActualizarCarrito(int userId, int productId, int quantity)
    { using var db = Open(); db.Execute("sp_Carrito_Actualizar", new { id_usuario=userId, id_producto=productId, cantidad=quantity }, commandType: CommandType.StoredProcedure); }

    public void AgregarCarrito(int userId, int productId, int quantity)
    { using var db=Open(); db.Execute("sp_Carrito_AgregarProducto", new { id_usuario=userId,id_producto=productId,cantidad=quantity }, commandType:CommandType.StoredProcedure); }

    public void EliminarCarrito(int userId, int? productId)
    { using var db = Open(); db.Execute("sp_Carrito_Eliminar", new { id_usuario=userId, id_producto=productId }, commandType: CommandType.StoredProcedure); }

    public IReadOnlyList<PedidoDto> Pedidos(int? orderId, int? userId, string? status, string? type, bool includeClosed, string? search)
    {
        using var db = Open();
        var orders = db.Query<PedidoDto>("sp_Pedido_Listar", new { id_pedido=orderId, id_usuario=userId, estado=status, tipo=type, incluir_cerrados=includeClosed, buscar=search }, commandType: CommandType.StoredProcedure).ToList();
        foreach (var order in orders)
        {
            order.Detalles = db.Query<PedidoDetalleDto>("sp_Pedido_Detalles", new { id_pedido=order.Id }, commandType: CommandType.StoredProcedure).ToList();
            order.Historial = db.Query<HistorialDto>("sp_Pedido_Historial", new { id_pedido=order.Id }, commandType: CommandType.StoredProcedure).ToList();
        }
        return orders;
    }

    public PedidoDto CrearPedido(int userId, CrearPedidoRequest request)
    {
        using var db=Open();
        var args=new DynamicParameters(); args.Add("id_usuario",userId); args.Add("tipo",request.TipoEntrega); args.Add("direccion",request.DireccionEntrega);
        args.Add("numero_pedido",dbType:DbType.String,size:30,direction:ParameterDirection.Output); args.Add("id_pedido",dbType:DbType.Int32,direction:ParameterDirection.Output);
        db.Execute("sp_Pedido_CrearWeb",args,commandType:CommandType.StoredProcedure);
        return Pedidos(args.Get<int>("id_pedido"),userId,null,null,true,null).Single();
    }

    public void CancelarPedido(int userId,int orderId)
    { using var db=Open(); db.Execute("sp_Pedido_Cancelar",new { id_pedido=orderId,id_usuario=userId },commandType:CommandType.StoredProcedure); }
    public void AvanzarPedido(int orderId)
    { using var db=Open(); db.Execute("sp_Pedido_AvanzarEstado",new { id_pedido=orderId },commandType:CommandType.StoredProcedure); }

    public IReadOnlyList<ProductoVendidoDto> ProductosVendidos(DateTime? from, DateTime? to, int? categoryId)
    { using var db=Open(); return db.Query<ProductoVendidoDto>("sp_Reporte_ProductosVendidos", new { desde=from?.Date, hasta=to?.Date, id_categoria=categoryId }, commandType:CommandType.StoredProcedure).ToList(); }
    public IReadOnlyList<MovimientoDto> Movimientos(DateTime? from, DateTime? to, int? productId)
    { using var db=Open(); return db.Query<MovimientoDto>("sp_Reporte_Movimientos", new { desde=from?.Date, hasta=to?.Date, id_producto=productId }, commandType:CommandType.StoredProcedure).ToList(); }
    public IReadOnlyList<AuditoriaDto> Auditoria()
    { using var db=Open(); return db.Query<AuditoriaDto>("sp_Auditoria_Listar", commandType:CommandType.StoredProcedure).ToList(); }
    public IReadOnlyList<ErrorDto> Errores()
    { using var db=Open(); return db.Query<ErrorDto>("sp_Error_Listar", commandType:CommandType.StoredProcedure).ToList(); }
    public void AgregarError(ErrorRequest request)
    { using var db=Open(); db.Execute("sp_Error_Agregar", new { ruta=request.Ruta, mensaje=request.Mensaje }, commandType:CommandType.StoredProcedure); }

    private sealed class ProductCartRow
    {
        public int Id { get; set; }
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int StockMinimo { get; set; }
        public string ImagenUrl { get; set; } = "";
        public bool Activo { get; set; }
        public DateTime ActualizadoUtc { get; set; }
        public int Cantidad { get; set; }
        public ProductoDto ToProduct() => new() { Id=Id, CategoriaId=CategoriaId, Nombre=Nombre, Descripcion=Descripcion, Precio=Precio, Stock=Stock, StockMinimo=StockMinimo, ImagenUrl=ImagenUrl, Activo=Activo, ActualizadoUtc=ActualizadoUtc };
    }
}
