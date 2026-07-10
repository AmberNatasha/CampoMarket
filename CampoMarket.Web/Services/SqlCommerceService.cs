using CampoMarket.Web.Models;
using Microsoft.Data.SqlClient;

namespace CampoMarket.Web.Services;

public sealed class SqlCommerceService(IConfiguration configuration, IUserRepository users) :
    ICartService,
    IOrderService,
    IReportService
{
    private readonly string _connectionString = configuration.GetConnectionString("CampoMarket")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:CampoMarket.");

    public IReadOnlyList<Pedido> Pedidos
    {
        get
        {
            using var connection = OpenConnection();
            return ReadOrders(connection, null, null, null, null, true);
        }
    }

    public IReadOnlyList<CarritoLineaViewModel> GetCart(int userId)
    {
        using var connection = OpenConnection();
        var cartId = EnsureCart(connection, null, userId);
        using var command = new SqlCommand("""
            SELECT p.id_producto, p.id_categoria, p.nombre_producto, ISNULL(p.descripcion, ''),
                   p.precio, p.stock, p.stock_minimo, ISNULL(p.imagen_url, ''), p.activo,
                   p.fecha_actualizacion, dc.cantidad
            FROM dbo.Detalle_Carrito dc
            INNER JOIN dbo.Producto p ON p.id_producto = dc.id_producto
            WHERE dc.id_carrito = @cartId
            ORDER BY p.nombre_producto;
            """, connection);
        command.Parameters.AddWithValue("@cartId", cartId);

        using var reader = command.ExecuteReader();
        var items = new List<CarritoLineaViewModel>();
        while (reader.Read())
        {
            items.Add(new CarritoLineaViewModel
            {
                Producto = ReadProduct(reader),
                Cantidad = reader.GetInt32(10)
            });
        }

        return items;
    }

    public (bool Ok, string Message) AddToCart(int userId, int productId, int cantidad)
    {
        if (cantidad <= 0) return (false, "La cantidad debe ser mayor a cero.");

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var stock = GetProductStock(connection, transaction, productId);
            if (stock is null) return (false, "Producto no encontrado.");

            var cartId = EnsureCart(connection, transaction, userId);
            var current = GetCartQuantity(connection, transaction, cartId, productId);
            var next = current + cantidad;
            if (next > stock.Value) return (false, "No hay stock suficiente.");

            UpsertCartItem(connection, transaction, cartId, productId, next);
            transaction.Commit();
            return (true, "Producto agregado al carrito.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void UpdateCart(int userId, int productId, int cantidad)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var cartId = EnsureCart(connection, transaction, userId);
            if (cantidad <= 0)
            {
                DeleteCartItem(connection, transaction, cartId, productId);
            }
            else
            {
                var stock = GetProductStock(connection, transaction, productId) ?? 0;
                UpsertCartItem(connection, transaction, cartId, productId, Math.Min(cantidad, stock));
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void RemoveFromCart(int userId, int productId)
    {
        using var connection = OpenConnection();
        var cartId = EnsureCart(connection, null, userId);
        Execute(connection, null, """
            DELETE FROM dbo.Detalle_Carrito
            WHERE id_carrito = @cartId AND id_producto = @productId;
            """,
            ("@cartId", cartId),
            ("@productId", productId));
    }

    public void ClearCart(int userId)
    {
        using var connection = OpenConnection();
        var cartId = EnsureCart(connection, null, userId);
        Execute(connection, null, "DELETE FROM dbo.Detalle_Carrito WHERE id_carrito = @cartId;", ("@cartId", cartId));
    }

    public (bool Ok, string Message, Pedido? Pedido) CreateOrder(int userId, string tipoEntrega, string direccionEntrega)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var committed = false;
        try
        {
            var cartId = EnsureCart(connection, transaction, userId);
            var items = ReadCartItems(connection, transaction, cartId);
            if (items.Count == 0) return (false, "Tu carrito esta vacio.", null);

            foreach (var item in items)
            {
                if (item.Cantidad > item.Producto.Stock)
                {
                    return (false, $"Stock insuficiente para {item.Producto.Nombre}.", null);
                }
            }

            var methodId = EnsureDeliveryMethod(connection, transaction, tipoEntrega);
            var addressId = ResolveAddress(connection, transaction, userId, tipoEntrega, direccionEntrega);
            var number = NextOrderNumber(connection, transaction);
            var total = items.Sum(i => i.Cantidad * i.Producto.Precio);

            using var orderCommand = new SqlCommand("""
                INSERT INTO dbo.Pedido (id_usuario, id_direccion, id_metodo, numero_pedido, estado, total)
                OUTPUT INSERTED.id_pedido
                VALUES (@userId, @addressId, @methodId, @number, @estado, @total);
                """, connection, transaction);
            orderCommand.Parameters.AddWithValue("@userId", userId);
            orderCommand.Parameters.AddWithValue("@addressId", addressId);
            orderCommand.Parameters.AddWithValue("@methodId", methodId);
            orderCommand.Parameters.AddWithValue("@number", number);
            orderCommand.Parameters.AddWithValue("@estado", EstadosPedido.Pendiente);
            orderCommand.Parameters.AddWithValue("@total", total);
            var orderId = (int)orderCommand.ExecuteScalar()!;

            foreach (var item in items)
            {
                Execute(connection, transaction, """
                    INSERT INTO dbo.Detalle_Pedido (id_pedido, id_producto, cantidad, precio_unitario)
                    VALUES (@orderId, @productId, @quantity, @price);

                    UPDATE dbo.Producto
                    SET stock = stock - @quantity
                    WHERE id_producto = @productId;

                    INSERT INTO dbo.Movimiento_Inventario (id_producto, tipo, cantidad, motivo)
                    VALUES (@productId, 'Venta automatica', @movementQuantity, @number);
                    """,
                    ("@orderId", orderId),
                    ("@productId", item.Producto.Id),
                    ("@quantity", item.Cantidad),
                    ("@price", item.Producto.Precio),
                    ("@movementQuantity", -item.Cantidad),
                    ("@number", number));
            }

            Execute(connection, transaction, "DELETE FROM dbo.Detalle_Carrito WHERE id_carrito = @cartId;", ("@cartId", cartId));
            transaction.Commit();
            committed = true;
            var pedido = ReadOrders(connection, orderId, null, null, null, true).FirstOrDefault();
            return (true, $"Pedido {number} generado.", pedido);
        }
        catch
        {
            if (!committed)
            {
                transaction.Rollback();
            }

            throw;
        }
    }

    public (bool Ok, string Message) CancelOrder(int userId, int orderId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var order = ReadOrders(connection, orderId, userId, null, null, true).FirstOrDefault();
            if (order is null) return (false, "Pedido no encontrado.");
            if (order.Estado != EstadosPedido.Pendiente) return (false, "Solo puedes cancelar pedidos pendientes.");

            Execute(connection, transaction, """
                UPDATE dbo.Pedido
                SET estado = @estado, fecha_cancelacion = GETDATE()
                WHERE id_pedido = @orderId;
                """,
                ("@orderId", orderId),
                ("@estado", EstadosPedido.Cancelado));

            foreach (var detail in order.Detalles)
            {
                Execute(connection, transaction, """
                    UPDATE dbo.Producto
                    SET stock = stock + @quantity
                    WHERE id_producto = @productId;

                    INSERT INTO dbo.Movimiento_Inventario (id_producto, tipo, cantidad, motivo)
                    VALUES (@productId, 'Reintegro por cancelacion', @quantity, @number);
                    """,
                    ("@productId", detail.ProductoId),
                    ("@quantity", detail.Cantidad),
                    ("@number", order.Numero));
            }

            transaction.Commit();
            return (true, "Pedido cancelado y stock reintegrado.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public (bool Ok, string Message) AdvanceOrder(int orderId)
    {
        using var connection = OpenConnection();
        var current = FindOrder(orderId);
        if (current is null) return (false, "Pedido no encontrado.");

        var next = current.Estado switch
        {
            EstadosPedido.Pendiente => EstadosPedido.Preparando,
            EstadosPedido.Preparando => EstadosPedido.Listo,
            EstadosPedido.Listo => EstadosPedido.Entregado,
            _ => null
        };
        if (next is null) return (false, "El pedido ya no puede avanzar.");

        Execute(connection, null, "UPDATE dbo.Pedido SET estado = @estado WHERE id_pedido = @orderId;", ("@orderId", orderId), ("@estado", next));
        return (true, $"Pedido actualizado a {next}.");
    }

    public IEnumerable<Pedido> PedidosCliente(int userId)
    {
        using var connection = OpenConnection();
        return ReadOrders(connection, null, userId, null, null, true);
    }

    public Pedido? FindOrder(int id)
    {
        using var connection = OpenConnection();
        return ReadOrders(connection, id, null, null, null, true).FirstOrDefault();
    }

    public IEnumerable<Pedido> BuscarPedidosAdmin(string? estado, string? tipo, string? buscar, bool incluirCerrados = false)
    {
        using var connection = OpenConnection();
        return ReadOrders(connection, null, null, estado, tipo, incluirCerrados)
            .Where(p => string.IsNullOrWhiteSpace(buscar)
                || p.Numero.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase)
                || (users.FindById(p.UsuarioId)?.Nombre.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    public Usuario? UsuarioPedido(int pedidoUsuarioId) => users.FindById(pedidoUsuarioId);

    public IEnumerable<ProductoVendidoViewModel> ProductosMasVendidos(DateTime? desde, DateTime? hasta, int? categoriaId)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            SELECT p.nombre_producto, SUM(dp.cantidad), SUM(dp.cantidad * dp.precio_unitario)
            FROM dbo.Detalle_Pedido dp
            INNER JOIN dbo.Pedido pe ON pe.id_pedido = dp.id_pedido
            INNER JOIN dbo.Producto p ON p.id_producto = dp.id_producto
            WHERE pe.estado <> @cancelado
              AND (@desde IS NULL OR CAST(pe.fecha_pedido AS date) >= @desde)
              AND (@hasta IS NULL OR CAST(pe.fecha_pedido AS date) <= @hasta)
              AND (@categoriaId IS NULL OR p.id_categoria = @categoriaId)
            GROUP BY p.nombre_producto
            ORDER BY SUM(dp.cantidad) DESC;
            """, connection);
        command.Parameters.AddWithValue("@cancelado", EstadosPedido.Cancelado);
        command.Parameters.AddWithValue("@desde", desde?.Date ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@hasta", hasta?.Date ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@categoriaId", categoriaId is > 0 ? categoriaId.Value : DBNull.Value);

        using var reader = command.ExecuteReader();
        var items = new List<ProductoVendidoViewModel>();
        while (reader.Read())
        {
            items.Add(new ProductoVendidoViewModel
            {
                Producto = reader.GetString(0),
                Cantidad = reader.GetInt32(1),
                Total = reader.GetDecimal(2)
            });
        }

        return items;
    }

    public IEnumerable<MovimientoInventario> FiltrarMovimientos(DateTime? desde, DateTime? hasta, int? productoId)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            SELECT m.id_producto, p.nombre_producto, m.tipo, m.cantidad, m.motivo, m.fecha_movimiento
            FROM dbo.Movimiento_Inventario m
            INNER JOIN dbo.Producto p ON p.id_producto = m.id_producto
            WHERE (@desde IS NULL OR CAST(m.fecha_movimiento AS date) >= @desde)
              AND (@hasta IS NULL OR CAST(m.fecha_movimiento AS date) <= @hasta)
              AND (@productoId IS NULL OR m.id_producto = @productoId)
            ORDER BY m.fecha_movimiento DESC;
            """, connection);
        command.Parameters.AddWithValue("@desde", desde?.Date ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@hasta", hasta?.Date ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@productoId", productoId is > 0 ? productoId.Value : DBNull.Value);

        using var reader = command.ExecuteReader();
        var items = new List<MovimientoInventario>();
        while (reader.Read())
        {
            items.Add(new MovimientoInventario
            {
                ProductoId = reader.GetInt32(0),
                ProductoNombre = reader.GetString(1),
                Tipo = reader.GetString(2),
                Cantidad = reader.GetInt32(3),
                Motivo = reader.GetString(4),
                FechaUtc = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Local).ToUniversalTime()
            });
        }

        return items;
    }

    private List<Pedido> ReadOrders(SqlConnection connection, int? orderId, int? userId, string? estado, string? tipo, bool incluirCerrados)
    {
        using var command = new SqlCommand("""
            SELECT pe.id_pedido, pe.numero_pedido, pe.id_usuario, pe.estado, me.tipo,
                   CONCAT(d.provincia, ', ', d.canton, ', ', d.distrito, '. ', d.senas_exactas),
                   pe.fecha_pedido, pe.fecha_cancelacion, pe.total
            FROM dbo.Pedido pe
            INNER JOIN dbo.Metodo_Entrega me ON me.id_metodo = pe.id_metodo
            INNER JOIN dbo.Direccion d ON d.id_direccion = pe.id_direccion
            WHERE (@orderId IS NULL OR pe.id_pedido = @orderId)
              AND (@userId IS NULL OR pe.id_usuario = @userId)
              AND (@estado IS NULL OR pe.estado = @estado)
              AND (@tipo IS NULL OR me.tipo = @tipo)
              AND (@incluirCerrados = 1 OR pe.estado NOT IN ('Entregado', 'Cancelado'))
            ORDER BY pe.fecha_pedido DESC;
            """, connection);
        command.Parameters.AddWithValue("@orderId", orderId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@userId", userId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(estado) ? DBNull.Value : estado);
        command.Parameters.AddWithValue("@tipo", string.IsNullOrWhiteSpace(tipo) ? DBNull.Value : tipo);
        command.Parameters.AddWithValue("@incluirCerrados", incluirCerrados);

        using var reader = command.ExecuteReader();
        var orders = new List<Pedido>();
        while (reader.Read())
        {
            orders.Add(new Pedido
            {
                Id = reader.GetInt32(0),
                Numero = reader.GetString(1),
                UsuarioId = reader.GetInt32(2),
                Estado = reader.GetString(3),
                TipoEntrega = reader.GetString(4),
                DireccionEntrega = reader.GetString(5),
                FechaUtc = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Local).ToUniversalTime(),
                CanceladoUtc = reader.IsDBNull(7) ? null : DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Local).ToUniversalTime(),
                Total = reader.GetDecimal(8)
            });
        }

        foreach (var order in orders)
        {
            order.Detalles = ReadOrderDetails(connection, order.Id);
            order.Historial = ReadOrderHistory(connection, order.Id);
        }

        return orders;
    }

    private static List<PedidoDetalle> ReadOrderDetails(SqlConnection connection, int orderId)
    {
        using var command = new SqlCommand("""
            SELECT dp.id_producto, p.nombre_producto, dp.cantidad, dp.precio_unitario
            FROM dbo.Detalle_Pedido dp
            INNER JOIN dbo.Producto p ON p.id_producto = dp.id_producto
            WHERE dp.id_pedido = @orderId
            ORDER BY p.nombre_producto;
            """, connection);
        command.Parameters.AddWithValue("@orderId", orderId);
        using var reader = command.ExecuteReader();
        var details = new List<PedidoDetalle>();
        while (reader.Read())
        {
            details.Add(new PedidoDetalle
            {
                ProductoId = reader.GetInt32(0),
                ProductoNombre = reader.GetString(1),
                Cantidad = reader.GetInt32(2),
                PrecioUnitario = reader.GetDecimal(3)
            });
        }

        return details;
    }

    private static List<HistorialEstado> ReadOrderHistory(SqlConnection connection, int orderId)
    {
        using var command = new SqlCommand("""
            SELECT estado, fecha_cambio
            FROM dbo.Historial_Estado_Pedido
            WHERE id_pedido = @orderId
            ORDER BY fecha_cambio;
            """, connection);
        command.Parameters.AddWithValue("@orderId", orderId);
        using var reader = command.ExecuteReader();
        var history = new List<HistorialEstado>();
        while (reader.Read())
        {
            history.Add(new HistorialEstado
            {
                Estado = reader.GetString(0),
                FechaUtc = DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Local).ToUniversalTime()
            });
        }

        return history;
    }

    private List<CarritoLineaViewModel> ReadCartItems(SqlConnection connection, SqlTransaction transaction, int cartId)
    {
        using var command = new SqlCommand("""
            SELECT p.id_producto, p.id_categoria, p.nombre_producto, ISNULL(p.descripcion, ''),
                   p.precio, p.stock, p.stock_minimo, ISNULL(p.imagen_url, ''), p.activo,
                   p.fecha_actualizacion, dc.cantidad
            FROM dbo.Detalle_Carrito dc
            INNER JOIN dbo.Producto p WITH (UPDLOCK, ROWLOCK) ON p.id_producto = dc.id_producto
            WHERE dc.id_carrito = @cartId;
            """, connection, transaction);
        command.Parameters.AddWithValue("@cartId", cartId);
        using var reader = command.ExecuteReader();
        var items = new List<CarritoLineaViewModel>();
        while (reader.Read())
        {
            items.Add(new CarritoLineaViewModel
            {
                Producto = ReadProduct(reader),
                Cantidad = reader.GetInt32(10)
            });
        }

        return items;
    }

    private static Producto ReadProduct(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        CategoriaId = reader.GetInt32(1),
        Nombre = reader.GetString(2),
        Descripcion = reader.GetString(3),
        Precio = reader.GetDecimal(4),
        Stock = reader.GetInt32(5),
        StockMinimo = reader.GetInt32(6),
        ImagenUrl = reader.GetString(7),
        Activo = reader.GetBoolean(8),
        ActualizadoUtc = DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Local).ToUniversalTime()
    };

    private static int EnsureCart(SqlConnection connection, SqlTransaction? transaction, int userId)
    {
        using (var find = new SqlCommand("SELECT id_carrito FROM dbo.Carrito WHERE id_usuario = @userId;", connection, transaction))
        {
            find.Parameters.AddWithValue("@userId", userId);
            if (find.ExecuteScalar() is int existing) return existing;
        }

        using var insert = new SqlCommand("INSERT INTO dbo.Carrito (id_usuario) OUTPUT INSERTED.id_carrito VALUES (@userId);", connection, transaction);
        insert.Parameters.AddWithValue("@userId", userId);
        return (int)insert.ExecuteScalar()!;
    }

    private static int? GetProductStock(SqlConnection connection, SqlTransaction? transaction, int productId)
    {
        using var command = new SqlCommand("SELECT stock FROM dbo.Producto WHERE id_producto = @productId AND activo = 1;", connection, transaction);
        command.Parameters.AddWithValue("@productId", productId);
        return command.ExecuteScalar() is int stock ? stock : null;
    }

    private static int GetCartQuantity(SqlConnection connection, SqlTransaction transaction, int cartId, int productId)
    {
        using var command = new SqlCommand("SELECT ISNULL(SUM(cantidad), 0) FROM dbo.Detalle_Carrito WHERE id_carrito = @cartId AND id_producto = @productId;", connection, transaction);
        command.Parameters.AddWithValue("@cartId", cartId);
        command.Parameters.AddWithValue("@productId", productId);
        return (int)command.ExecuteScalar()!;
    }

    private static void UpsertCartItem(SqlConnection connection, SqlTransaction transaction, int cartId, int productId, int quantity)
    {
        if (quantity <= 0)
        {
            DeleteCartItem(connection, transaction, cartId, productId);
            return;
        }

        Execute(connection, transaction, """
            IF EXISTS (SELECT 1 FROM dbo.Detalle_Carrito WHERE id_carrito = @cartId AND id_producto = @productId)
                UPDATE dbo.Detalle_Carrito SET cantidad = @quantity WHERE id_carrito = @cartId AND id_producto = @productId;
            ELSE
                INSERT INTO dbo.Detalle_Carrito (id_carrito, id_producto, cantidad) VALUES (@cartId, @productId, @quantity);
            """,
            ("@cartId", cartId),
            ("@productId", productId),
            ("@quantity", quantity));
    }

    private static void DeleteCartItem(SqlConnection connection, SqlTransaction transaction, int cartId, int productId) =>
        Execute(connection, transaction, "DELETE FROM dbo.Detalle_Carrito WHERE id_carrito = @cartId AND id_producto = @productId;", ("@cartId", cartId), ("@productId", productId));

    private static int EnsureDeliveryMethod(SqlConnection connection, SqlTransaction transaction, string type)
    {
        var normalized = type == TiposEntrega.Recoleccion ? TiposEntrega.Recoleccion : TiposEntrega.Express;
        using (var find = new SqlCommand("SELECT id_metodo FROM dbo.Metodo_Entrega WHERE tipo = @type;", connection, transaction))
        {
            find.Parameters.AddWithValue("@type", normalized);
            if (find.ExecuteScalar() is int existing) return existing;
        }

        using var insert = new SqlCommand("INSERT INTO dbo.Metodo_Entrega (tipo, costo_adicional) OUTPUT INSERTED.id_metodo VALUES (@type, @cost);", connection, transaction);
        insert.Parameters.AddWithValue("@type", normalized);
        insert.Parameters.AddWithValue("@cost", normalized == TiposEntrega.Recoleccion ? 0m : 2.50m);
        return (int)insert.ExecuteScalar()!;
    }

    private static int ResolveAddress(SqlConnection connection, SqlTransaction transaction, int userId, string type, string addressText)
    {
        if (type == TiposEntrega.Recoleccion)
        {
            return EnsureSystemAddress(connection, transaction, userId, "Campo Market", "Central", "Tienda", "Campo Market Central, 8:00 a 20:00");
        }

        using var command = new SqlCommand("""
            SELECT TOP 1 id_direccion
            FROM dbo.Direccion
            WHERE id_usuario = @userId
              AND CONCAT(provincia, ', ', canton, ', ', distrito, '. ', senas_exactas) = @address
            ORDER BY predeterminada DESC, id_direccion;
            """, connection, transaction);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@address", addressText);
        if (command.ExecuteScalar() is int existing) return existing;

        using var fallback = new SqlCommand("""
            SELECT TOP 1 id_direccion
            FROM dbo.Direccion
            WHERE id_usuario = @userId
            ORDER BY predeterminada DESC, id_direccion;
            """, connection, transaction);
        fallback.Parameters.AddWithValue("@userId", userId);
        if (fallback.ExecuteScalar() is int fallbackId) return fallbackId;

        return EnsureSystemAddress(connection, transaction, userId, "Sin provincia", "Sin canton", "Sin distrito", string.IsNullOrWhiteSpace(addressText) ? "Direccion no especificada" : addressText);
    }

    private static int EnsureSystemAddress(SqlConnection connection, SqlTransaction transaction, int userId, string provincia, string canton, string distrito, string senas)
    {
        using var insert = new SqlCommand("""
            INSERT INTO dbo.Direccion (id_usuario, provincia, canton, distrito, senas_exactas, predeterminada)
            OUTPUT INSERTED.id_direccion
            VALUES (@userId, @provincia, @canton, @distrito, @senas, 0);
            """, connection, transaction);
        insert.Parameters.AddWithValue("@userId", userId);
        insert.Parameters.AddWithValue("@provincia", provincia);
        insert.Parameters.AddWithValue("@canton", canton);
        insert.Parameters.AddWithValue("@distrito", distrito);
        insert.Parameters.AddWithValue("@senas", senas);
        return (int)insert.ExecuteScalar()!;
    }

    private static string NextOrderNumber(SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand("SELECT NEXT VALUE FOR dbo.SQ_NumeroPedido;", connection, transaction);
        var next = (int)command.ExecuteScalar()!;
        return $"CM-{DateTime.Now:yyyyMMdd}-{next:0000}";
    }

    private SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void Execute(SqlConnection connection, SqlTransaction? transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
    }
}
