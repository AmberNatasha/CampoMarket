using CampoMarket.Web.Models;
using Microsoft.Data.SqlClient;

namespace CampoMarket.Web.Services;

public interface ICatalogRepository
{
    CatalogState Load();
    void Save(CatalogState state);
    DatabaseConnectionInfo GetConnectionInfo();
}

public sealed class DatabaseConnectionInfo
{
    public string Server { get; set; } = "";
    public string Database { get; set; } = "";
    public string Login { get; set; } = "";
}

public sealed class SqlCatalogRepository(IConfiguration configuration) : ICatalogRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("CampoMarket")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:CampoMarket.");

    public CatalogState Load()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var state = new CatalogState();

        using (var command = new SqlCommand("""
            SELECT id_categoria, nombre_categoria, ISNULL(descripcion, ''), activo
            FROM dbo.Categoria
            ORDER BY id_categoria;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                state.Categorias.Add(new Categoria
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Descripcion = reader.GetString(2),
                    Activa = reader.GetBoolean(3)
                });
            }
        }

        using (var command = new SqlCommand("""
            SELECT id_producto, id_categoria, nombre_producto, ISNULL(descripcion, ''), precio,
                   stock, stock_minimo, ISNULL(imagen_url, ''), activo, fecha_actualizacion
            FROM dbo.Producto
            ORDER BY id_producto;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                state.Productos.Add(new Producto
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
                });
            }
        }

        using (var command = new SqlCommand("""
            SELECT m.id_producto, p.nombre_producto, m.tipo, m.cantidad, m.motivo, m.fecha_movimiento
            FROM dbo.Movimiento_Inventario m
            INNER JOIN dbo.Producto p ON p.id_producto = m.id_producto
            ORDER BY m.fecha_movimiento;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                state.Movimientos.Add(new MovimientoInventario
                {
                    ProductoId = reader.GetInt32(0),
                    ProductoNombre = reader.GetString(1),
                    Tipo = reader.GetString(2),
                    Cantidad = reader.GetInt32(3),
                    Motivo = reader.GetString(4),
                    FechaUtc = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Local).ToUniversalTime()
                });
            }
        }

        return state;
    }

    public DatabaseConnectionInfo GetConnectionInfo()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = new SqlCommand("""
            SELECT
                CAST(SERVERPROPERTY('ServerName') AS NVARCHAR(128)),
                DB_NAME(),
                SUSER_SNAME();
            """, connection);
        using var reader = command.ExecuteReader();
        reader.Read();

        return new DatabaseConnectionInfo
        {
            Server = reader.GetString(0),
            Database = reader.GetString(1),
            Login = reader.GetString(2)
        };
    }

    public void Save(CatalogState state)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            UpsertCategories(connection, transaction, state.Categorias);
            UpsertProducts(connection, transaction, state.Productos);
            ReplaceMovements(connection, transaction, state.Movimientos);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void UpsertCategories(SqlConnection connection, SqlTransaction transaction, IEnumerable<Categoria> categories)
    {
        foreach (var category in categories)
        {
            var exists = Exists(connection, transaction, "dbo.Categoria", "id_categoria", category.Id);
            if (exists)
            {
                Execute(connection, transaction, """
                    UPDATE dbo.Categoria
                    SET nombre_categoria = @nombre, descripcion = @descripcion, activo = @activo
                    WHERE id_categoria = @id;
                    """,
                    ("@id", category.Id),
                    ("@nombre", category.Nombre),
                    ("@descripcion", category.Descripcion),
                    ("@activo", category.Activa));
            }
            else
            {
                Execute(connection, transaction, """
                    SET IDENTITY_INSERT dbo.Categoria ON;
                    INSERT INTO dbo.Categoria (id_categoria, nombre_categoria, descripcion, activo)
                    VALUES (@id, @nombre, @descripcion, @activo);
                    SET IDENTITY_INSERT dbo.Categoria OFF;
                    """,
                    ("@id", category.Id),
                    ("@nombre", category.Nombre),
                    ("@descripcion", category.Descripcion),
                    ("@activo", category.Activa));
            }
        }
    }

    private static void UpsertProducts(SqlConnection connection, SqlTransaction transaction, IEnumerable<Producto> products)
    {
        foreach (var product in products)
        {
            var exists = Exists(connection, transaction, "dbo.Producto", "id_producto", product.Id);
            if (exists)
            {
                Execute(connection, transaction, """
                    UPDATE dbo.Producto
                    SET nombre_producto = @nombre,
                        descripcion = @descripcion,
                        precio = @precio,
                        stock = @stock,
                        imagen_url = @imagen,
                        id_categoria = @categoriaId,
                        activo = @activo,
                        stock_minimo = @stockMinimo,
                        fecha_actualizacion = GETDATE()
                    WHERE id_producto = @id;
                    """,
                    ProductParameters(product));
            }
            else
            {
                Execute(connection, transaction, """
                    SET IDENTITY_INSERT dbo.Producto ON;
                    INSERT INTO dbo.Producto (id_producto, nombre_producto, descripcion, precio, stock, imagen_url, id_categoria, activo, stock_minimo, fecha_actualizacion)
                    VALUES (@id, @nombre, @descripcion, @precio, @stock, @imagen, @categoriaId, @activo, @stockMinimo, GETDATE());
                    SET IDENTITY_INSERT dbo.Producto OFF;
                    """,
                    ProductParameters(product));
            }
        }
    }

    private static void ReplaceMovements(SqlConnection connection, SqlTransaction transaction, IEnumerable<MovimientoInventario> movements)
    {
        Execute(connection, transaction, "DELETE FROM dbo.Movimiento_Inventario;");

        foreach (var movement in movements)
        {
            Execute(connection, transaction, """
                INSERT INTO dbo.Movimiento_Inventario (id_producto, tipo, cantidad, motivo, fecha_movimiento)
                VALUES (@productoId, @tipo, @cantidad, @motivo, @fecha);
                """,
                ("@productoId", movement.ProductoId),
                ("@tipo", movement.Tipo),
                ("@cantidad", movement.Cantidad),
                ("@motivo", movement.Motivo),
                ("@fecha", movement.FechaUtc.ToLocalTime()));
        }
    }

    private static (string Name, object? Value)[] ProductParameters(Producto product) =>
    [
        ("@id", product.Id),
        ("@nombre", product.Nombre),
        ("@descripcion", product.Descripcion),
        ("@precio", product.Precio),
        ("@stock", product.Stock),
        ("@imagen", string.IsNullOrWhiteSpace(product.ImagenUrl) ? DBNull.Value : product.ImagenUrl),
        ("@categoriaId", product.CategoriaId),
        ("@activo", product.Activo),
        ("@stockMinimo", product.StockMinimo)
    ];

    private static bool Exists(SqlConnection connection, SqlTransaction transaction, string table, string idColumn, int id)
    {
        using var command = new SqlCommand($"SELECT COUNT(1) FROM {table} WHERE {idColumn} = @id;", connection, transaction);
        command.Parameters.AddWithValue("@id", id);
        return (int)command.ExecuteScalar()! > 0;
    }

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
    }
}

public sealed class CatalogState
{
    public List<Categoria> Categorias { get; set; } = [];
    public List<Producto> Productos { get; set; } = [];
    public List<MovimientoInventario> Movimientos { get; set; } = [];
}
